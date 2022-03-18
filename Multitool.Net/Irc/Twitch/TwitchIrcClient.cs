using Multitool.Net.Irc.Factories;
using Multitool.Net.Irc.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Irc.Twitch
{
    public class TwitchIrcClient : IIrcClient
    {
        #region fields
        #region constants fields
        private const int bufferSize = 16_000;
        private const int charReplacement = 0;
        private const int sendSemaphoreWait = 1_000;
        private const string wssUri = @"wss://irc-ws.chat.twitch.tv:443";
        #endregion

        private readonly Dictionary<CommandType, Regex> commands = new();
        private readonly TwitchConnectionToken _connectionToken;
        private readonly MessageFactory factory = new() { UseLocalTimestamp = false };
        private readonly Thread receiveThread;
        private readonly CancellationTokenSource rootCancelToken = new();
        private readonly bool silentExit;
        private readonly SemaphoreSlim webSocketSendSemaphore = new(1);
        private readonly SemaphoreSlim webSocketReceiveSemaphore = new(1);

        private string channel;
        private bool disposed;
        private bool hasJoined;
        private ClientWebSocket socket;

        // thread safe attributes -- using 64 bits adresses because programm is compiled for x64
        private long disconnected = 1;
        private long loggedIn = 0;
        #endregion

        #region constructor
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="login">Twitch 2OAuth token</param>
        /// <param name="silentExit"><see langword="true"/> to exit the websocket background thread without throwing any exceptions.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="login"/> is null.</exception>
        public TwitchIrcClient(TwitchConnectionToken login, bool silentExit)
        {
            // assert login
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            _connectionToken = login;
            this.silentExit = silentExit;
#if true
            receiveThread = new(ReceiveData)
            {
                IsBackground = true
            };
#else
            receiveThread = new Thread(() => { }); 
#endif

            commands.Add(CommandType.JOIN, IrcRegexes.JoinRegex);
            commands.Add(CommandType.NAMES, IrcRegexes.NamesRegex);
            commands.Add(CommandType.PING, IrcRegexes.PingRegex);
            commands.Add(CommandType.USERSTATE, IrcRegexes.UserStateRegex);
            commands.Add(CommandType.ROOMSTATE, IrcRegexes.RoomStateRegex);
            commands.Add(CommandType.CLEARCHAT, IrcRegexes.ClearChatRegex);
            commands.Add(CommandType.USERNOTICE, IrcRegexes.UserNoticeRegex);
        }
        #endregion

        #region events
        /// <inheritdoc/>
        public event TypedEventHandler<IIrcClient, Message> MessageReceived;
        /// <inheritdoc/>
        public event TypedEventHandler<IIrcClient, EventArgs> Disconnected;
        /// <inheritdoc/>
        public event TypedEventHandler<IIrcClient, RoomStateEventArgs> RoomChanged;
        /// <inheritdoc/>
        public event TypedEventHandler<IIrcClient, UserTimeoutEventArgs> UserTimedOut;
        /// <inheritdoc/>
        public event TypedEventHandler<IIrcClient, UserNoticeEventArgs> UserNotice;
        #endregion

        #region properties
        /// <inheritdoc/>
        public bool AutoLogIn { get; init; }

        /// <inheritdoc/>
        public WebSocketState ClientState => socket == null ? WebSocketState.None : socket.State;

        /// <inheritdoc/>
        public ConnectionToken ConnectionToken => _connectionToken;

        /// <inheritdoc/>
        public Encoding Encoding { get; set; } = Encoding.Default;

        /// <inheritdoc/>
        public bool IsConnected => Interlocked.Read(ref disconnected) == 0;

        /// <inheritdoc/>
        public string NickName { get; set; }

        /// <inheritdoc/>
        public CancellationTokenSource RootCancellationToken => rootCancelToken;
        #endregion

        #region IIrcClient
        /// <inheritdoc/>
        public async Task Disconnect()
        {
            if (Interlocked.Read(ref disconnected) == 0)
            {
                Interlocked.Exchange(ref disconnected, 1);
                RootCancellationToken.Cancel();
                await webSocketReceiveSemaphore.WaitAsync();
                if (socket != null)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None); 
                }
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
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());

                    }
                }

                try
                {
                    if (socket != null)
                    {
                        socket.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                webSocketReceiveSemaphore.Dispose();
                webSocketSendSemaphore.Dispose();

                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <inheritdoc/>
        public async Task Join(string channel)
        {
            if (Encoding == null)
            {
                throw new NullReferenceException("Encoding cannot be null to decode data from the socket connection");
            }
            if (!ConnectionToken.Validated)
            {
                throw new ArgumentException("Connection token has not been validated");
            }
            AssertChannelNameValid(channel);

            if (string.IsNullOrWhiteSpace(NickName))
            {
                NickName = _connectionToken.Login;
            }
            try
            {
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

                    await LogIn();
                }

                await SendStringAsync($"JOIN #{channel}");
                string response = await ReceiveStringAsync();
                if (!IrcRegexes.JoinRegex.IsMatch(response))
                {
                    throw new InvalidOperationException($"Failed to join {channel}");
                }
                else
                {
                    Debug.WriteLine(response);
                }

                hasJoined = true;
                receiveThread.Start();
                this.channel = channel;
            }
            catch (WebSocketException)
            {
                Trace.TraceError($"{nameof(TwitchIrcClient)} failed to join {channel}. Disposing websocket.");
                socket.Dispose();
                socket = null;
                throw;
            }
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
        public async Task SendMessage(string message)
        {
            AssertConnectionValid();
            await SendStringAsync($"PRIVMSG #{channel} :{message}");
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }

        public void Subscribe(IIrcSubscriber subscriber)
        {
            Disconnected += subscriber.OnDisconnected;
            MessageReceived += subscriber.OnMessageReceived;
            RoomChanged += subscriber.OnRoomChanged;
            UserTimedOut += subscriber.OnUserTimedOut;
            UserNotice += subscriber.OnUserNotice;
        }
        #endregion

        #region private methods
        #region irc
        private async Task Connect()
        {
            if (Interlocked.Read(ref disconnected) == 1)
            {
                if (socket == null)
                {
                    socket = new();
                }
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

        private void RouteMessage(ReadOnlyMemory<char> message)
        {
            string s = message.ToString();
            if (IrcRegexes.MessageRegex.IsMatch(s))
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

        private void OnMessageReceived(ReadOnlyMemory<char> message)
        {
            if (hasJoined)
            {
                Message received = factory.CreateMessage(message);
                InvokeMessageReceived(received);
            }
        }

        private void OnCommandReceived(CommandType tag, ReadOnlyMemory<char> command)
        {
            int index = 0;
            Dictionary<string, string> tags = MessageFactory.ParseTags(command, ref index);

            switch (tag)
            {
                case CommandType.NAMES:
                    // /NAMES
                    //Match match = namesRegex.Match(command.ToString());
                    Trace.TraceInformation($"NAMES command: {command}");
                    break;

                case CommandType.PING:
                    socket.SendAsync(GetBytes("PONG"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
                    break;

                case CommandType.USERSTATE:
                    StringBuilder builder = new();
                    foreach (var t in tags)
                    {
                        builder.AppendLine($"@{t.Key} = {t.Value},");
                    }
                    Debug.WriteLine("> USERSTATE\n" + builder.ToString());
                    break;

                case CommandType.ROOMSTATE:
                    OnRoomStateChange(tags);
                    break;

                case CommandType.CLEARCHAT:
                    string user = tags["target-user-id"];
                    ReadOnlyMemory<char> remains = command[index..];
                    int i = 1;
                    for (; i < remains.Length; i++)
                    {
                        if (remains.Span[i] == ':')
                        {
                            i++;
                            break;
                        }
                    }
                    string name = remains[i..].ToString();
                    UserTimeoutEventArgs eventArgs = new()
                    {
                        User = factory.UserFactory.GetUser(user),
                        UserName = name
                    };

                    int banDuration = 0;
                    if (tags.TryGetValue("ban-duration", out string banDurationTag))
                    {
                        banDuration = int.Parse(banDurationTag);
                        eventArgs.Timeout = TimeSpan.FromSeconds(banDuration);
                    }

                    UserTimedOut?.Invoke(this, eventArgs);
                    break;

                case CommandType.USERNOTICE:
                    UserNotice?.Invoke(this, MessageFactory.CreateUserNotice(command, tags, index));
                    break;

                case CommandType.NOTICE:
                    Debug.WriteLine($"IRC NOTICE: {command}");
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
            if (await webSocketSendSemaphore.WaitAsync(sendSemaphoreWait))
            {
                try
                {
                    if (socket != null)
                    {
                        await socket.SendAsync(GetBytes(message), WebSocketMessageType.Text, end, RootCancellationToken.Token); 
                    }
                }
                catch (WebSocketException)
                {
                    Interlocked.Exchange(ref disconnected, 1);
                    throw;
                }
                finally
                {
                    webSocketSendSemaphore.Release();
                }
            }
            else
            {
                Trace.TraceWarning($"Failed to send {message}");
            }
        }

        private async Task<string> ReceiveStringAsync()
        {
            ArraySegment<byte> buffer = new(new byte[bufferSize]);
            try
            {
                if (socket != null)
                {
                    CancellationTokenSource token = new(5_000);
                    try
                    {
                        await webSocketReceiveSemaphore.WaitAsync();
                        WebSocketReceiveResult res = await socket.ReceiveAsync(buffer, token.Token);
                        token.Cancel();
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
                    finally
                    {
                        webSocketReceiveSemaphore.Release();
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (WebSocketException)
            {
                Interlocked.Exchange(ref disconnected, 1);
                Disconnected?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        private async void ReceiveData(object obj)
        {
            ArraySegment<byte> data = new(new byte[bufferSize]);
            List<ReadOnlyMemory<char>> commandMessages = new(5);
            int errorCount = 0;
            while (Interlocked.Read(ref disconnected) == 0)
            {
                try
                {
                    AssertConnectionValid();
                    // for some reason we cannot cancel outbounds packets
                    try
                    {
                        await webSocketReceiveSemaphore.WaitAsync();
                        await socket.ReceiveAsync(data, RootCancellationToken.Token);
                    }
                    finally
                    {
                        webSocketReceiveSemaphore.Release();
                    }

                    if (data.Count != 0)
                    {
                        int upperBound = 0;
                        for (; upperBound < data.Count; upperBound++)
                        {
                            if (data[upperBound] == 0x0)
                            {
                                break;
                            }
                            else if (data[upperBound] == 0xF3)
                            {
                                // to clear weird continuation byte chars
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

                        //ArraySegment<byte> sliced = data.Slice(0, upperBound);
                        char[] message = Encoding.GetChars(data.Array, 0, upperBound);
                        ReadOnlyMemory<char> readOnlyMessage = MemoryExtensions.AsMemory(message);

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
                catch (WebSocketException ex)
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
                    errorCount++;
                    if (errorCount > 10)
                    {
                        Interlocked.Exchange(ref disconnected, 1);
                        Trace.TraceError($"Caught exception (count: {errorCount}) in receive thread. Exiting thread.\n{ex}");
                    }
                    else
                    {
                        Trace.TraceError($"Caught exception (count: {errorCount}) in receive thread : \n{ex}");
                    }
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
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
                throw ClientState switch
                {
                    WebSocketState.None => new WebSocketException(WebSocketError.InvalidState, "Client has never connected"),
                    WebSocketState.Connecting => new WebSocketException(WebSocketError.InvalidState, "Client is connecting"),
                    WebSocketState.CloseSent => new WebSocketException(WebSocketError.InvalidState, "Client is closing it's connection"),
                    WebSocketState.CloseReceived => new WebSocketException(WebSocketError.InvalidState, "Client has received close request from the connection endpoint"),
                    WebSocketState.Closed => new WebSocketException(WebSocketError.InvalidState, "Client connection is closed"),
                    WebSocketState.Aborted => new WebSocketException(WebSocketError.InvalidState, $"{nameof(ClientState)} == WebSocketState.Aborted")
                };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
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

    enum CommandType : short
    {
        JOIN = 1,
        NAMES = 2,
        PING = 3,
        USERSTATE = 4,
        ROOMSTATE = 5,
        CLEARCHAT = 6,
        USERNOTICE = 7,
        NOTICE = 8
    }
}