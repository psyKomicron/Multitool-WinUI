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
        private const string wssUri = @"wss://irc-ws.chat.twitch.tv:443";

        #region static regexes
        // commands
        private static readonly Regex joinRegex = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv JOIN .))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex namesRegex = new(@"^:(.+)\.tmi\.twitch\.tv 353 \1 = #[a-z0-9]+ :");
        private static readonly Regex pingRegex = new(@"^PING", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        // message
        private static readonly Regex messageRegex = new(@".+ *PRIVMSG .+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex userStateRegex = new(@"USERSTATE", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex roomStateRegex = new(@"ROOMSTATE", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        #endregion

        private readonly int bufferSize;
        private readonly bool silentExit;
        private readonly CancellationTokenSource rootCancelToken = new();
        private readonly ClientWebSocket socket = new();
        private readonly MessageFactory factory = new() { UseLocalTimestamp = false };
        private readonly Dictionary<uint, Regex> commands = new();
        private readonly Thread receiveThread;

        private bool disposed;
        private bool hasJoined;
        // thread safe attributes -- using 64 bits adresses because programm is compiled for x64
        private long disconnected = 1;
        private long loggedIn = 0;
        #endregion

        #region constructor
        public TwitchIrcClient(TwitchConnectionToken login, int bufferSize, bool silentExit)
        {
            // assert login
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            ConnectionToken = login;
            this.bufferSize = bufferSize;
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
        }
        #endregion

        #region events
        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, Message> MessageReceived;

        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, EventArgs> Disconnected;

        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, string> Joined;

        /// <inheritdoc/>
        public event TypedEventHandler<ITwitchIrcClient, RoomStates> RoomChanged;
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
        public bool RequestTags { get; init; }

        /// <inheritdoc/>
        public CancellationTokenSource RootCancellationToken => rootCancelToken;
        #endregion

        #region public methods
        /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task SendMessage(string message)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            AssertConnectionValid();

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task Join(string channel)
        {
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
                await LogIn();
            }
    
            await SendStringAsync($"JOIN #{channel}");
            string response = await ReceiveStringAsync();
            if (!joinRegex.IsMatch(response))
            {
                throw new InvalidOperationException($"Failed to join {channel}");
            }
            else
            {
                hasJoined = true;
                receiveThread.Start();
            }
        }

        /// <inheritdoc/>
        public async Task Part(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);
            if (hasJoined)
            {
                await SendStringAsync($"PART #{channel}");
                hasJoined = false;
            }
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
                socket.Dispose();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }
        #endregion

        #region non-public methods

        #region socket methods
        private ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.GetBytes(text));
        }

        private ArraySegment<byte> GetBytes(Memory<char> text)
        {
            return new(Encoding.GetBytes(text.ToArray()));
        }

        private async Task SendStringAsync(string message, bool end = true)
        {
            try
            {
                await socket.SendAsync(GetBytes(message), WebSocketMessageType.Text, end, RootCancellationToken.Token);
            }
            catch (WebSocketException)
            {
                Interlocked.Exchange(ref disconnected, 1);
                Disconnected?.Invoke(this, EventArgs.Empty);
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
                switch (ClientState)
                {
                    case WebSocketState.None:
                        throw new InvalidOperationException("Client has never connected");
                    case WebSocketState.Connecting:
                        throw new InvalidOperationException("Client is connecting");
                    case WebSocketState.CloseSent:
                        throw new InvalidOperationException("Client is closing it's connection");
                    case WebSocketState.CloseReceived:
                        throw new InvalidOperationException("Client has received close request from the connection endpoint");
                    case WebSocketState.Closed:
                        throw new InvalidOperationException("Client connection is closed");
                    case WebSocketState.Aborted:
                        throw new InvalidOperationException($"{nameof(ClientState)} == WebSocketState.Aborted");
                    default:
                        throw new ArgumentException($"Unkown WebSocketState state, argument name : {nameof(ClientState)}");
                }
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

        private async Task Connect()
        {
            if (Interlocked.Read(ref disconnected) == 1)
            {
                await socket.ConnectAsync(new(wssUri), RootCancellationToken.Token);
                Interlocked.Exchange(ref disconnected, 0);

                AssertConnectionValid();
            }
        }

        /// <summary>
        /// Do not start the receive thread before calling this method. It will cause the socket to break.
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        /// <exception cref="InvalidOperationException">Thrown if any of the calls to the Twitch IRC server. The exception message will be explicit.</exception>
        private async Task LogIn()
        {
            if (RequestTags)
            {
                Regex nak = new(@"NAK");
                await SendStringAsync(@"CAP REQ :twitch.tv/membership");
                string rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request tags capability from tmi.twitch.tv");
                }

                await SendStringAsync(@"CAP REQ :twitch.tv/commands");
                rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request tags capability from tmi.twitch.tv");
                }

                await SendStringAsync(@"CAP REQ :twitch.tv/tags");
                rep = await ReceiveStringAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Failed to request tags capability from tmi.twitch.tv");
                }
            }

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
#if !DEBUG
            Task.Run(() => MessageReceived?.Invoke(this, message));
#else
            MessageReceived?.Invoke(this, message);
#endif
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
            int index = default;
            Dictionary<string, string> tags = MessageFactory.ParseTags(command, ref index);
            
            switch (tag)
            {
                case 2:
                    // /NAMES
                    //Match match = namesRegex.Match(command.ToString());
                    Trace.TraceInformation($"NAMES command: {command}");
                    break;

                case 3:
                    const string pong = "PONG";
                    socket.SendAsync(GetBytes(pong), WebSocketMessageType.Text, true, RootCancellationToken.Token);
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
                    RoomStates changes = RoomStates.None;
                    if (tags.ContainsKey("followers-only"))
                    {
                        changes = tags["followers-only"] == "1" ? RoomStates.FollowersOnlyOn : RoomStates.FollowersOnlyOff;
                    }
                    if (tags.ContainsKey("emote-only"))
                    {
                        changes = tags["emote-only"] == "1" ? RoomStates.EmoteOnlyOn : RoomStates.EmoteOnlyOff;
                    }
                    if (tags.ContainsKey("r9k"))
                    {
                        changes = tags["r9k"] == "1" ? RoomStates.R9KOn : RoomStates.R9KOff;
                    }
                    if (tags.ContainsKey("slow"))
                    {
                        changes = tags["slow"] == "1" ? RoomStates.SlowModeOn : RoomStates.SlowModeOff;
                    }
                    if (tags.ContainsKey("subs-only"))
                    {
                        changes = tags["subs-only"] == "1" ? RoomStates.SubsOnlyOn : RoomStates.SubsOnlyOff;
                    }

                    RoomChanged?.Invoke(this, changes);
                    break;
                default:
#if DEBUG
                    Debug.WriteLine("> Dropping: " + command.ToString());
                    break;
#else
                    throw new ArgumentOutOfRangeException(nameof(tag));
#endif
            }
        }

        private async void ReceiveData(object obj)
        {
            ArraySegment<byte> data = new(new byte[bufferSize]);
            while (Interlocked.Read(ref disconnected) == 0)
            {
                try
                {
                    AssertConnectionValid();
                    await socket.ReceiveAsync(data, CancellationToken.None);
                    if (data.Count != 0)
                    {
                        int max = 0;
                        for (; max < data.Count; max++)
                        {
                            if (data[max] == 0x0)
                            {
                                break;
                            }
                            else if (data[max] == 0x10)
                            {
                                data[max] = 0x40;
                            }
                        }

                        ArraySegment<byte> sliced = data.Slice(0, max);
                        string message = Encoding.GetString(sliced);
                        ReadOnlyMemory<char> readOnlyMessage = MemoryExtensions.AsMemory(message);
#if false
                        Debug.WriteLine(message);
#endif
                        if (messageRegex.IsMatch(message))
                        {
                            try
                            {
                                OnMessageReceived(readOnlyMessage);
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
                            List<ReadOnlyMemory<char>> commandMessages = new();
                            int sliceIndex = 0;
                            for (int i = 0; i < readOnlyMessage.Length; i++)
                            {
                                if (readOnlyMessage.Span[i] == '\n')
                                {
                                    ReadOnlyMemory<char> c = readOnlyMessage[sliceIndex..(i - 1)];
                                    commandMessages.Add(c);
                                    sliceIndex = i + 1;
                                }
                            }

                            foreach (ReadOnlyMemory<char> commandMessage in commandMessages)
                            {
                                foreach (var command in commands)
                                {
                                    if (command.Value.IsMatch(commandMessage.ToString()))
                                    {
                                        try
                                        {
                                            OnCommandReceived(command.Key, commandMessage);
                                        }
#if DEBUG
                                        catch (Exception ex)
                                        {
                                            Trace.TraceError("\n\tOnCommandReceived > " + ex.ToString());
                                        }
#else
                                    catch { }
#endif
                                        break;
                                    }
                                }
                            }
                        }

                        // clear buffer
                        for (int i = 0; i < max; i++)
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
                    }
                }
                catch (WebSocketException ex) // thread will exit (and break the application) when a websocket exception occur.
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
                    Trace.TraceError(ex.ToString());
                }
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
