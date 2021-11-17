using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Irc
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
                IsBackground = true
            };
        }

        public event TypedEventHandler<IIrcClient, string> MessageReceived;

        #region properties
        public CancellationTokenSource RootCancellationToken => rootCancelToken;
        public WebSocketState ClientState => Socket.State;
        public bool Connected => Interlocked.Read(ref disconnected) == 0;
        public string NickName { get; set; }
        public Encoding Encoding { get; set; }

        protected bool Disposed => disposed;
        protected ClientWebSocket Socket { get; } = new();
        protected Thread ReceiveThread => receiveThread;
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

        public async Task Disconnect()
        {
            if (Interlocked.Read(ref disconnected) == 0)
            {
                Interlocked.Exchange(ref disconnected, 1);
                RootCancellationToken.Cancel();
                await Socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
                await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "User initiated", CancellationToken.None);
                Trace.TraceInformation("IRC client disconnected");
            }
            else
            {
                Trace.TraceWarning($"IRC client already disconnected. Client status : {ClientState}");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CheckDisposed();
            rootCancelToken.Cancel();
            Disconnect()
                .ContinueWith((Task t) =>
                {
                    Socket.Dispose();
                    disposed = true;
                });
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }
        #endregion

        #region protected methods
        protected abstract void OnMessageReceived(Span<char> message);

        /// <summary>
        /// Asserts that the connection is not disposed and open.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the socket is not open (<see cref="WebSocketState.Open"/>)</exception>
        /// <exception cref="ArgumentException">Thrown when the socket state is not recognized (default clause in the switch)</exception>
        protected void AssertConnectionValid()
        {
            CheckDisposed();
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

        protected void InvokeMessageReceived(string message)
        {
#if !DEBUG
            Task.Run(() => MessageReceived?.Invoke(this, message));
#else
            MessageReceived?.Invoke(this, message);
#endif
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
            Trace.TraceInformation("Starting IRC client receive background thread");

            ArraySegment<byte> data = new(new byte[bufferSize]);
            do
            {
                try
                {
                    await Socket.ReceiveAsync(data, CancellationToken.None);
                    if (data.Count != 0)
                    {
#if DEBUG
                        StringBuilder dataAsString = new();
#endif
                        int max = 0;
                        for (; max < data.Count; max++)
                        {
                            if (data[max] == 0x0)
                            {
                                break;
                            }
#if DEBUG
                            dataAsString.Append(data[max] + " ");
#endif
                        }
                        ArraySegment<byte> sliced = data.Slice(0, max);
                        string message = Encoding.GetString(sliced);
#if false
                        Debug.WriteLine(dataAsString.ToString());
#endif
#if DEBUG
                        Debug.WriteLine(message);
                        // clear buffer
#endif
                        OnMessageReceived(new(message.ToCharArray()));
                        for (int i = 0; i < max; i++)
                        {
                            if (data[i] != 0)
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
                    if (!silentExit)
                    {
                        Trace.TraceError("WebSocket exception occured, exiting receive thread.\n" + ex.ToString());
                        throw;
                    }
                    else
                    {
                        Trace.TraceError("WebSocket exception occured, exiting receive thread silently.\n" + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            while (Interlocked.Read(ref disconnected) == 0);
            Trace.TraceInformation($"IrcClient '{NickName}' receive thread exiting");
        }
        #endregion
    }
}