using Multitool.Net.Twitch.Factories;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Twitch.Irc
{
    public class TwitchIrcClient : ITwitchIrcClient
    {
        #region fields
        #region static regexes
        // commands
        private static readonly Regex joinRegex = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv JOIN .))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex namesRegex = new(@"^:(.+)\.tmi\.twitch\.tv 353 \1 = #[a-z0-9]+ :");
        private static readonly Regex pingRegex = new(@"^PING", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        // message
        private static readonly Regex messageRegex = new(@".+ *PRIVMSG .+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex userStateRegex = new("USERSTATE", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex roomStateRegex = new("ROOMSTATE", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex clearChatRegex = new("CLEARCHAT", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        #endregion
        private const int bufferSize = 16_000;
        private const int charReplacement = 0;
        private const int sendSemaphoreWait = 1_000;
        private const string wssUri = @"wss://irc-ws.chat.twitch.tv:443";

        private readonly Dictionary<uint, Regex> commands = new();
        private readonly MessageFactory factory = new() { UseLocalTimestamp = false };
        private readonly Thread receiveThread;
        private readonly CancellationTokenSource rootCancelToken = new();
        private readonly ClientWebSocket socket = new();
        private readonly bool silentExit;

        private SemaphoreSlim webSocketReceiveSemaphore = new(1);
        private SemaphoreSlim webSocketSendSemaphore = new(1);

        private string channel;
        private bool disposed;
        private bool hasJoined;
        //private string channel;
        // thread safe attributes -- using 64 bits adresses because programm is compiled for x64
        private long disconnected = 1;
        private long loggedIn = 0;
        #endregion

        #region constructor
        public TwitchIrcClient(TwitchConnectionToken login, bool silentExit)
        {
            // assert login
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            ConnectionToken = login;
            this.silentExit = silentExit;
            receiveThread = new(ReceiveData)
            {
                IsBackground = false
            };
            ConnectionToken = login;

            commands.Add(1, joinRegex);
            commands.Add(2, namesRegex);
            commands.Add(3, pingRegex);
            commands.Add(4, userStateRegex);
            commands.Add(5, roomStateRegex);
            commands.Add(6, clearChatRegex);
        }
        #endregion

        #region properties
        /// <inheritdoc/>
        public bool AutoLogIn { get; init; }

        /// <inheritdoc/>
        public WebSocketState ClientState => socket.State;

        /// <inheritdoc/>
        public TwitchConnectionToken ConnectionToken { get; set; }

        /// <inheritdoc/>
        public Encoding Encoding { get; set; }

        /// <inheritdoc/>
        public bool IsConnected => Interlocked.Read(ref disconnected) == 0;

        /// <inheritdoc/>
        public string NickName { get; set; }

        /// <inheritdoc/>
        public CancellationTokenSource RootCancellationToken => rootCancelToken;
        #endregion

        #region events
        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, Message> MessageReceived;

        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, EventArgs> Disconnected;

        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, RoomStateEventArgs> RoomChanged;
        #endregion

        #region public methods
        /// <inheritdoc/>
        public async Task SendMessage(string message)
        {
            AssertConnectionValid();
            await SendStringAsync($"PRIVMSG #{channel} :{message}");
        }

        /// <inheritdoc/>
        public async Task Join(string channel)
        {
            if (!ConnectionToken.Validated)
            {
                throw new ArgumentException("Connection token has not been validated");
            }
            AssertChannelNameValid(channel);

            if (Interlocked.Read(ref disconnected) == 1)
            {
                await Connect();
            }
            else
            {
                AssertConnectionValid();
            }

            if (Interlocked.Read(ref loggedIn) == 0)
            {
                Regex nak = new(@"NAK");
                await SendStringAsync(@"CAP REQ :twitch.tv/membership");
                string rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request membership capability from tmi.twitch.tv");
                }

                await SendStringAsync(@"CAP REQ :twitch.tv/commands");
                rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request commands capability from tmi.twitch.tv");
                }

                await SendStringAsync(@"CAP REQ :twitch.tv/tags");
                rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request tags capability from tmi.twitch.tv");
                }

                /*await SendStringAsync(@"CAP LS 302");
                rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request chathistory capability from tmi.twitch.tv");
                }
                else
                {
                    Debug.WriteLine(rep);
                }*/

                await LogIn();
            }

            await SendStringAsync($"JOIN #{channel}");
            string response = await ReceiveStringAsync();
            if (!joinRegex.IsMatch(response))
            {
                throw new InvalidOperationException($"Failed to join {channel}");
            }

            hasJoined = true;
            receiveThread.Start();
            this.channel = channel;
        }

        /// <inheritdoc/>
        public async Task Part(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);
            await SendStringAsync($"PART #{channel}");
            hasJoined = false;
        }

        /// <inheritdoc/>
        public async Task Disconnect()
        {
            if (Interlocked.Read(ref disconnected) == 0)
            {
                Interlocked.Exchange(ref disconnected, 1);

                RootCancellationToken.Cancel();
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
            }
            else
            {
                Trace.TraceWarning($"IRC client already disconnected. Client status : {ClientState}");
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                rootCancelToken.Cancel();

                if (Interlocked.Read(ref disconnected) == 0)
                {
                    try
                    {
                        await Disconnect();
                    }
                    catch (WebSocketException ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }

                try
                {
                    socket.Dispose();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                disposed = true;
                //GC.SuppressFinalize(this);
            }
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }
        #endregion

        #region non-public methods
        #region irc
        private async Task Connect()
        {
            if (Interlocked.Read(ref disconnected) == 1)
            {
                await socket.ConnectAsync(new(wssUri), RootCancellationToken.Token);
                Interlocked.Exchange(ref disconnected, 0);
                AssertConnectionValid();
            }
        }

        private async Task LogIn()
        {
            await SendStringAsync($"PASS {ConnectionToken}");
            await SendStringAsync($"NICK {NickName}");

            Task<string> response = ReceiveStringAsync();
            Regex authFailedRegex = new(@"^(:tmi.twitch.tv NOTICE \* :Login authentication failed)");
            await response;
            if (authFailedRegex.IsMatch(response.Result))
            {
                await Disconnect();
                throw new InvalidOperationException($"Authentication failed. Twitch IRC server responded with '{response.Result}'. IRC client was disconnected");
            }
            else
            {
                Interlocked.Exchange(ref loggedIn, 1);
            }
        }

        private void InvokeMessageReceived(Message message)
        {
            MessageReceived?.Invoke(this, message);
        }

        private void OnMessageReceived(ReadOnlyMemory<char> message)
        {
            if (hasJoined)
            {
                Message received = factory.CreateMessage(message);
                InvokeMessageReceived(received);
            }
        }

        private void OnCommandReceived(uint tag, ReadOnlyMemory<char> command)
        {
            int index = 0;
            Dictionary<string, string> tags = MessageFactory.ParseTags(command, ref index);

            switch (tag)
            {
                case 2:
                    // /NAMES
                    //Match match = namesRegex.Match(command.ToString());
                    Trace.TraceInformation($"NAMES command: {command}");
                    break;

                case 3:
                    socket.SendAsync(GetBytes("PONG"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
                    break;

                case 4:
                    StringBuilder builder = new();
                    foreach (var t in tags)
                    {
                        builder.AppendLine($"@{t.Key} = {t.Value},");
                    }
                    Debug.WriteLine("> USERSTATE\n" + builder.ToString());
                    break;

                case 5:
                    OnRoomStateChange(tags);
                    break;
                case 6:
                    var banDuration = tags["ban-duration"];
                    var user = tags["target-user-id"];
                    var remains = command[index..];
                    int i = 1;
                    for (; i < remains.Length; i++)
                    {
                        if (remains.Span[i] == ':')
                        {
                            i++;
                            break;
                        }
                    }
                    var name = remains[i..];
                    Debug.WriteLine($"{name}({user}) banned/timeout for {banDuration}");
                    break;
            }
        }

        private void OnRoomStateChange(Dictionary<string, string> tags)
        {
            RoomStateEventArgs args = new();
            if (tags.ContainsKey("followers-only"))
            {
                if (tags["followers-only"] == "0")
                {
                    args.States = RoomStates.FollowersOnlyOff;
                }
                else
                {
                    args.Data.Add(RoomStates.FollowersOnlyOn, int.Parse(tags["followers-only"]));
                    args.States = RoomStates.FollowersOnlyOn;
                }
            }

            if (tags.ContainsKey("emote-only"))
            {
                if (tags["emote-only"] == "0")
                {
                    args.States |= RoomStates.EmoteOnlyOff;
                }
                else
                {
                    args.Data.Add(RoomStates.EmoteOnlyOn, int.Parse(tags["emote-only"]));
                    args.States |= RoomStates.EmoteOnlyOn;
                }
            }

            if (tags.ContainsKey("slow"))
            {
                if (tags["slow"] == "0")
                {
                    args.States = RoomStates.SlowModeOff;
                }
                else
                {
                    args.Data.Add(RoomStates.SlowModeOff, int.Parse(tags["slow"]));
                    args.States = RoomStates.SlowModeOff;
                }
            }

            if (tags.ContainsKey("r9k"))
            {
                args.States |= tags["r9k"][0] == '1' ? RoomStates.R9KOn : RoomStates.R9KOff;
            }

            if (tags.ContainsKey("subs-only"))
            {
                args.States |= tags["subs-only"][0] == '1' ? RoomStates.SubsOnlyOn : RoomStates.SubsOnlyOff;
            }

            RoomChanged?.Invoke(this, args);
        }
        #endregion

        #region socket methods
        private ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.GetBytes(text));
        }

        private async Task SendStringAsync(string message, bool end = true)
        {
            try
            {
                if (await webSocketSendSemaphore.WaitAsync(sendSemaphoreWait))
                {
                    try
                    {
                        await socket.SendAsync(GetBytes(message), WebSocketMessageType.Text, end, RootCancellationToken.Token);
                    }
                    finally
                    {
                        webSocketSendSemaphore.Release();
                    }
                }
                else
                {
                    Trace.TraceWarning($"Failed to get send {message}");
                }
            }
            catch (WebSocketException)
            {
                Interlocked.Exchange(ref disconnected, 1);
                throw;
            }
        }

        private async Task<string> ReceiveStringAsync()
        {
            ArraySegment<byte> buffer = new(new byte[bufferSize]);
            try
            {
                WebSocketReceiveResult res = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Text)
                {
                    int max = 0;
                    for (; max < buffer.Count; max++)
                    {
                        if (buffer[max] == 0x0)
                        {
                            break;
                        }
                    }
                    buffer = buffer.Slice(0, max);
                    return Encoding.GetString(buffer);
                }
                else
                {
                    throw new InvalidOperationException("Message type wasn't text, decoding not possible.");
                }
            }
            catch (WebSocketException)
            {
                Interlocked.Exchange(ref disconnected, 1);
                Disconnected?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        private void RouteMessage(ReadOnlyMemory<char> message)
        {
            string s = message.ToString();
            if (messageRegex.IsMatch(s))
            {
                try
                {
                    OnMessageReceived(message);
                }
#if DEBUG
                catch (Exception ex)
                {
                    Trace.TraceError("\n\tOnMessageReceived > " + ex.ToString());
                }
#else
                catch { }
#endif
            }
            else
            {
                foreach (var command in commands)
                {
                    if (command.Value.IsMatch(s))
                    {
                        try
                        {
                            OnCommandReceived(command.Key, message);
                        }
#if DEBUG
                        catch (Exception ex)
                        {
                            Trace.TraceError("\n\tOnCommandReceived > " + ex.ToString());
                        }
#else
                        catch { }
#endif
                        return;
                    }
                }
                Debug.WriteLine($"Dropping : {s}");
            }
        }

        private async void ReceiveData(object obj)
        {
            ArraySegment<byte> data = new(new byte[bufferSize]);
            List<ReadOnlyMemory<char>> commandMessages = new(5);
            while (Interlocked.Read(ref disconnected) == 0)
            {
                try
                {
                    AssertConnectionValid();
                    await socket.ReceiveAsync(data, CancellationToken.None);
                    if (data.Count != 0)
                    {
#if DEBUG
                        int upperBound = 0;
                        for (; upperBound < data.Count; upperBound++)
                        {
                            if (data[upperBound] == 0x0)
                            {
                                break;
                            }
                            else if (data[upperBound] == 0xF3)
                            {
                                if (upperBound++ < data.Count && data[upperBound] == 0xA0)
                                {
                                    if (upperBound++ < data.Count && data[upperBound] == 0x80)
                                    {
                                        if (upperBound++ < data.Count && data[upperBound] == 0x80)
                                        {
                                            data[upperBound - 3] = charReplacement;
                                            data[upperBound - 2] = charReplacement;
                                            data[upperBound - 1] = charReplacement;
                                            data[upperBound] = charReplacement;
                                        }
                                    }
                                }
                            }
                        }

                        ArraySegment<byte> sliced = data.Slice(0, upperBound);
                        string message = Encoding.GetString(sliced);
#else
                        string message = Encoding.GetString(data);
#endif
                        ReadOnlyMemory<char> readOnlyMessage = MemoryExtensions.AsMemory(message);

                        /*if (b1 == 10 && b2 == 13 && b3 == 56320 && b4 == 56128)
                        {
                            readOnlyMessage = readOnlyMessage.Slice(0, readOnlyMessage.Length - 5);
                        }
                        for (int n = 0; n < readOnlyMessage.Length; n++)
                        {
                            if (readOnlyMessage.Span[n] == 56320 || readOnlyMessage.Span[n] == 56128)
                            {
                                readOnlyMessage = readOnlyMessage.Slice(0, )
                            }
                        }*/

                        int sliceIndex = 0;
                        for (int i = 0; i < readOnlyMessage.Length; i++)
                        {
                            if (sliceIndex < (i - 1) && readOnlyMessage.Span[i] == '\n')
                            {
                                try
                                {
                                    ReadOnlyMemory<char> c = readOnlyMessage[sliceIndex..(i - 1)];
                                    commandMessages.Add(c);
                                    sliceIndex = i + 1;
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"Cannot slice {message}\n{ex}");
                                }
                            }
                        }

                        for (int i = 0; i < commandMessages.Count; i++)
                        {
                            RouteMessage(commandMessages[i]);
                        }

                        // clear buffer
                        for (int i = 0; i < data.Count; i++)
                        {
                            if (data[i] != 0x0)
                            {
                                data[i] = default;
                            }
                            else
                            {
                                break;
                            }
                        }
                        commandMessages.Clear();
                    }
                }
                catch (WebSocketException ex) // thread will exit (and break the application ? maybe not in the last versions) when a websocket exception occur.
                {
                    Interlocked.Exchange(ref disconnected, 1);
                    if (silentExit)
                    {
                        Trace.TraceWarning("WebSocket exception occured, exiting receive thread silently.\n" + ex.ToString());
                    }
                    else
                    {
                        Trace.TraceError("WebSocket exception occured, exiting receive thread.\n" + ex.ToString());
                        Disconnected?.Invoke(this, EventArgs.Empty);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Caught exception in receive thread : \n{ex}");
                }
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region assertion methods
        /// <summary>
        /// Asserts that the connection is not disposed and open.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the socket is not open (<see cref="WebSocketState.Open"/>)</exception>
        /// <exception cref="ArgumentException">Thrown when the socket state is not recognized (default clause in the switch)</exception>
        protected void AssertConnectionValid()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (Interlocked.Read(ref disconnected) == 1)
            {
                throw new InvalidOperationException("Client has disconnected");
            }

            if (ClientState != WebSocketState.Open)
            {
                throw ClientState switch
                {
                    WebSocketState.None => new InvalidOperationException("Client has never connected"),
                    WebSocketState.Connecting => new InvalidOperationException("Client is connecting"),
                    WebSocketState.CloseSent => new InvalidOperationException("Client is closing it's connection"),
                    WebSocketState.CloseReceived => new InvalidOperationException("Client has received close request from the connection endpoint"),
                    WebSocketState.Closed => new InvalidOperationException("Client connection is closed"),
                    WebSocketState.Aborted => new InvalidOperationException($"{nameof(ClientState)} == WebSocketState.Aborted"),
                    _ => new ArgumentException($"Unkown WebSocketState state, argument name : {nameof(ClientState)}"),
                };
            }
        }

        /// <summary>
        /// Asserts that the channel name is valid.
        /// </summary>
        /// <param name="channel"></param>
        /// <exception cref="ArgumentException">Thrown if the channel name is not valid (will carry the value of <paramref name="channel"/>)</exception>
        protected static void AssertChannelNameValid(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"Channel name cannot be empty (value: \"{channel}\")", nameof(channel));
            }
        }
        #endregion
        #endregion
    }
}