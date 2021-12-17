
using Multitool.Net.Properties;
using Multitool.Net.Twitch.Factories;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Web.Http;

namespace Multitool.Net.Twitch.Irc
{
    public class TwitchIrcClient : ITwitchIrcClient
    {
        #region attributes
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
        private readonly Thread receiveThread;
        private readonly ClientWebSocket socket = new();
        private readonly TwitchConnectionToken login;
        private readonly MessageFactory factory = new();
        private readonly Dictionary<uint, Regex> commands = new();

        private bool disposed;
        private long disconnected = 1;
        private bool loggedIn;
        private bool hasJoined;
        //private string alias;
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
            this.login = login;

            commands.Add(1, joinRegex);
            commands.Add(2, namesRegex);
            commands.Add(3, pingRegex);
            commands.Add(4, userStateRegex);
            commands.Add(5, roomStateRegex);
        }
        #endregion

        #region events
        public event TypedEventHandler<ITwitchIrcClient, Message> MessageReceived;
        public event TypedEventHandler<ITwitchIrcClient, EventArgs> Connected;
        public event TypedEventHandler<ITwitchIrcClient, EventArgs> Disconnected;
        #endregion

        #region properties
        /// <inheritdoc/>
        public bool AutoConnect { get; init; }

        /// <inheritdoc/>
        public bool RequestTags { get; init; }

        /// <inheritdoc/>
        public CancellationTokenSource RootCancellationToken => rootCancelToken;

        /// <inheritdoc/>
        public WebSocketState ClientState => socket.State;

        /// <inheritdoc/>
        public bool IsConnected => Interlocked.Read(ref disconnected) == 0;

        /// <inheritdoc/>
        public string NickName { get; set; }

        /// <inheritdoc/>
        public Encoding Encoding { get; set; }

        /// <inheritdoc/>
        public TwitchConnectionToken ConnectionToken { get; init; }
        #endregion

        #region public methods
        /// <inheritdoc/>
        public async Task SendMessage(string message)
        {
            throw new NotImplementedException();
            AssertConnectionValid();

            //Debug.WriteLine($"{message} -> {data}\n");
            Debug.WriteLine(string.Empty);
        }

        /// <inheritdoc/>
        public async Task Join(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);
            if (!loggedIn)
            {
                await LogIn();
            }
    
            Trace.TraceInformation("Trying to join " + channel);
            await socket.SendAsync(GetBytes($"JOIN #{channel}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            Trace.TraceInformation("> Joined " + channel);
            hasJoined = true;
        }

        /// <inheritdoc/>
        public async Task Part(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);
            if (hasJoined)
            {
                await socket.SendAsync(GetBytes($"PART #{channel}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
                hasJoined = false;
                Trace.TraceInformation("> Left " + channel);
            }
            else
            {
                Trace.TraceInformation("> Already left channel");
            }
        }

        /// <inheritdoc/>
        public async Task Connect(Uri uri)
        {
            await Connect(uri, RootCancellationToken.Token);
        }

        /// <inheritdoc/>
        public async Task Connect(Uri channel, CancellationToken cancellationToken)
        {
            await socket.ConnectAsync(channel, cancellationToken);
            Interlocked.Exchange(ref disconnected, 0);

            AssertConnectionValid();

            if (AutoConnect)
            {
                await LogIn();
            }
            Connected?.Invoke(this, EventArgs.Empty);
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

                Trace.TraceInformation("IRC client disconnected");
                Disconnected?.Invoke(this, EventArgs.Empty);
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
                    await Disconnect();
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

        #region private methods

        #region socket methods
        private ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.GetBytes(text));
        }

        private ArraySegment<byte> GetBytes(Memory<char> text)
        {
            return new(Encoding.GetBytes(text.ToArray()));
        }

        private async Task SendAsync(string message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool end = true)
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

        private async Task<string> ReceiveAsync()
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
        private void AssertConnectionValid()
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
        private static void AssertChannelNameValid(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException($"Channel name cannot be empty (value: \"{channel}\")", nameof(channel));
            }
        }
        #endregion

        private async Task LogIn()
        {
            if (RequestTags)
            {
                Regex nak = new(@"NAK");
                await SendAsync(@"CAP REQ :twitch.tv/membership");
                string rep = await ReceiveAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Unable to request tags capability from tmi.twitch.tv");
                }
                else
                {
                    Trace.TraceInformation("ACK CAP REQ MEMBERSHIP");
                }

                await SendAsync(@"CAP REQ :twitch.tv/commands");
                rep = await ReceiveAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Unable to request tags capability from tmi.twitch.tv");
                }
                else
                {
                    Trace.TraceInformation("ACK CAP REQ COMMANDS");
                }

                await SendAsync(@"CAP REQ :twitch.tv/tags");
                rep = await ReceiveAsync();
                if (nak.IsMatch(rep))
                {
                    await Disconnect();
                    throw new InvalidOperationException("Unable to request tags capability from tmi.twitch.tv");
                }
                else
                {
                    Trace.TraceInformation("ACK CAP REQ TAGS");
                }
            }

            await SendAsync($"PASS {login}");
            await SendAsync($"NICK {NickName}");

            Task<string> response = ReceiveAsync();
            Regex authFailedRegex = new(@"^(:tmi.twitch.tv NOTICE \* :Login authentication failed)");
            await response;
            if (authFailedRegex.IsMatch(response.Result))
            {
                await Disconnect();
                throw new InvalidOperationException("Authentication failed");
            }
            else
            {
                loggedIn = true;

                receiveThread.Start();
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

        private void ParseMessage(Memory<char> message)
        {
            int i = 1;
            if (message.Span[0] == ':')
            {
                int nameLength = 0;
                for (; i < message.Length; i++)
                {
                    if (message.Span[i] == '@')
                    {
                        break;
                    }
                    else if (message.Span[i] == '!')
                    {
                        nameLength = i;
                    }
                }

                Memory<char> name = message[1..nameLength];

                while (i < message.Length && message.Span[i] != ':')
                {
                    i++;
                }

                Message m = factory.GetMessage(message);
                InvokeMessageReceived(m);
            }
        }

        private void OnMessageReceived(Memory<char> message)
        {
            if (hasJoined)
            {
#if DEBUG
                InvokeMessageReceived(factory.GetMessage(message));
#else
                if (messageRegex.IsMatch(message.ToString()))
                {
                    ParseMessage(message);
                }
                else
                {
                    Debug.WriteLine("> message : " + message.ToString());
                }
#endif
            }
        }

        private void OnCommandReceived(uint tag, Memory<char> command)
        {
            switch (tag)
            {
                case 1:
                    // JOIN
                    int i = 0;
                    for (; i < command.Length; i++)
                    {
                        if (command.Span[i] == '#')
                        {
                            break;
                        }
                    }
                    Trace.TraceInformation($"> Joined {command.Slice(command.Length - i - 1, i - 1)}");
                    break;
                case 2:
                    // /NAMES
                    //Match match = namesRegex.Match(command.ToString());
                    Trace.TraceInformation($"NAMES command: {command}");
                    break;
                case 3:
                    Trace.TraceInformation("Pong-ing twitch");
                    command.Span[1] = 'O';
                    socket.SendAsync(GetBytes(command), WebSocketMessageType.Text, true, RootCancellationToken.Token);
                    break;
                case 4:
                    Debug.WriteLine("USERSTATE");
                    break;
                case 5:
                    Debug.WriteLine("ROOMSTATE");
                    break;
                default:
#if DEBUG
                    Debug.WriteLine("Dropping: " + command.ToString());
                    break;
#else
                    throw new ArgumentOutOfRangeException(nameof(tag));
#endif
            }
        }

        private async void ReceiveData(object obj)
        {
            Trace.TraceInformation("Starting IRC client receive background thread");

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
                        }
                        ArraySegment<byte> sliced = data.Slice(0, max);
                        string message = Encoding.GetString(sliced);
#if false
                        Debug.WriteLine(message);
#endif
                        if (messageRegex.IsMatch(message))
                        {
#if false
                            _ = Task.Run(() => OnMessageReceived(new(message.ToCharArray())));
#else
                            OnMessageReceived(new(message.ToCharArray()));
#endif
                        }
                        else
                        {
                            foreach (var command in commands)
                            {
                                if (command.Value.IsMatch(message))
                                {
#if false
                                _ = Task.Run(() => OnCommandReceived(command.Key, new(message.ToCharArray())));
#else
                                    OnCommandReceived(command.Key, new(message.ToCharArray()));
#endif
                                    break;
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
                        Trace.TraceError("WebSocket exception occured, exiting receive thread silently.\n" + ex.ToString());
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
            Trace.TraceInformation($"IrcClient '{NickName}' receive thread exiting");

            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
