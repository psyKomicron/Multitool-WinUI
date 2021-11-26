using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Twitch.Irc
{
    public abstract class IrcClient : IIrcClient
    {
        private readonly int bufferSize;
        private readonly bool silentExit;
        private readonly CancellationTokenSource rootCancelToken = new();
        private readonly Thread receiveThread;
        private bool disposed;

        protected long disconnected = 1;

        protected IrcClient(int bufferSize, bool silentExit)
        {
            this.bufferSize = bufferSize;
            this.silentExit = silentExit;
            receiveThread = new(ReceiveData)
            {
                IsBackground = false
            };
        }

        public event TypedEventHandler<IIrcClient, Message> MessageReceived;
        public event TypedEventHandler<IIrcClient, EventArgs> Connected;
        public event TypedEventHandler<IIrcClient, EventArgs> Disconnected;

        #region properties
        public CancellationTokenSource RootCancellationToken => rootCancelToken;
        public WebSocketState ClientState => Socket.State;
        public bool IsConnected => Interlocked.Read(ref disconnected) == 0;
        public string NickName { get; set; }
        public Encoding Encoding { get; set; }

        protected bool Disposed => disposed;
        protected ClientWebSocket Socket { get; } = new();
        protected Thread ReceiveThread => receiveThread;
        protected Dictionary<uint, Regex> Commands { get; set; } = new();
        #endregion

        #region public methods
        /// <inheritdoc/>
        public abstract Task SendMessage(string message);
        /// <inheritdoc/>
        public abstract Task Join(string channel);
        /// <inheritdoc/>
        public abstract Task Part(string channel);

        /// <inheritdoc/>
        public virtual async Task Connect(Uri uri)
        {
            await Socket.ConnectAsync(uri, RootCancellationToken.Token);
            Interlocked.Exchange(ref disconnected, 0);
        }

        /// <inheritdoc/>
        public async Task Connect(Uri channel, CancellationToken cancellationToken)
        {
            await Socket.ConnectAsync(channel, cancellationToken);
            Interlocked.Exchange(ref disconnected, 0);
        }

        /// <inheritdoc/>
        public async Task Disconnect()
        {
            if (Interlocked.Read(ref disconnected) == 0)
            {
                Interlocked.Exchange(ref disconnected, 1);

                RootCancellationToken.Cancel();

                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);

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
                Socket.Dispose();
                disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }
        #endregion

        #region protected methods
        protected abstract void OnMessageReceived(Span<char> message);

        protected abstract void OnCommandReceived(uint tag, Span<char> command);

        /// <summary>
        /// Asserts that the connection is not disposed and open.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the socket is not open (<see cref="WebSocketState.Open"/>)</exception>
        /// <exception cref="ArgumentException">Thrown when the socket state is not recognized (default clause in the switch)</exception>
        protected void AssertConnectionValid()
        {
            CheckDisposed();
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

        protected void InvokeMessageReceived(Message message)
        {
#if !DEBUG
            Task.Run(() => MessageReceived?.Invoke(this, message));
#else
            MessageReceived?.Invoke(this, message);
#endif
        }

        protected ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.GetBytes(text));
        }

        protected ArraySegment<byte> GetBytes(Span<char> text)
        {
            return new(Encoding.GetBytes(text.ToArray()));
        }

        protected async Task SendAsync(string message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool end = true)
        {
            try
            {
                await Socket.SendAsync(GetBytes(message), WebSocketMessageType.Text, end, RootCancellationToken.Token);
            }
            catch (WebSocketException)
            {
                Interlocked.Exchange(ref disconnected, 1);
                Disconnected?.Invoke(this, EventArgs.Empty);
                throw;
            }
        }

        protected async Task<string> ReceiveAsync()
        {
            ArraySegment<byte> buffer = new(new byte[bufferSize]);
            try
            {
                WebSocketReceiveResult res = await Socket.ReceiveAsync(buffer, CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Text)
                {
                    int max = 0;
                    for (int i; max < buffer.Count; max++)
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

        #region private methods
        private void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private async void ReceiveData(object obj)
        {
#if false
            Trace.TraceWarning("Not starting receive thread");
            return;
            throw new OperationCanceledException();
#endif
            Trace.TraceInformation("Starting IRC client receive background thread");

            ArraySegment<byte> data = new(new byte[bufferSize]);
            bool isCommand;

            while (Interlocked.Read(ref disconnected) == 0)
            {
                try
                {
                    isCommand = false;
                    AssertConnectionValid();
                    await Socket.ReceiveAsync(data, CancellationToken.None);
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
                        foreach (var command in Commands)
                        {
                            if (command.Value.IsMatch(message))
                            {
                                isCommand = true;
#if false
                                _ = Task.Run(() => OnCommandReceived(command.Key, new(message.ToCharArray())));
#else
                                OnCommandReceived(command.Key, new(message.ToCharArray()));
#endif
                                break;
                            }
                        }
                        if (!isCommand)
                        {
#if false
                            _ = Task.Run(() => OnMessageReceived(new(message.ToCharArray())));
#else
                            OnMessageReceived(new(message.ToCharArray()));
#endif
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