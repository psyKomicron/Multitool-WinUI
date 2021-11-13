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
        private const int bufferSize = 5000;
        private readonly CancellationTokenSource rootCancelToken = new();
        private readonly Thread receiveThread;
        private bool disposed;
        protected long disconnected;

        public IrcClient()
        {
            receiveThread = new(ReceiveData)
            {
                IsBackground = true
            };
        }

        public event TypedEventHandler<IIrcClient, string> MessageReceived;

        #region properties
        public CancellationTokenSource RootCancellationToken => rootCancelToken;

        public WebSocketState ClientState => Socket.State;

        public bool Connected { get; protected set; }

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
            Connected = true;
        }

        /// <inheritdoc/>
        public async Task Connect(Uri channel, CancellationToken cancellationToken)
        {
            await Socket.ConnectAsync(channel, cancellationToken);
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
                .ContinueWith((Task t) => Socket.Dispose());
            disposed = true;
        }

        public CancellationTokenSource GetCancellationToken()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(rootCancelToken.Token);
        }
        #endregion

        #region protected methods
        protected abstract void OnMessageReceived(Span<char> message);

        protected void AssertConnectionValid()
        {
            if (ClientState != WebSocketState.Open)
            {
                switch (ClientState)
                {
                    case WebSocketState.None:
                        throw new InvalidOperationException("Client status is unknown");
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

        protected static void AssertChannelNameValid(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                throw new ArgumentException("Channel name cannot be empty", nameof(channel));
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
        protected void CheckDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private async void ReceiveData(object obj)
        {
            ArraySegment<byte> data = new(new byte[bufferSize]);
            do
            {
                try
                {
                    await Socket.ReceiveAsync(data, CancellationToken.None);
                    StringBuilder dataAsString = new();
                    int i = 0;
                    for (; i < data.Count; i++)
                    {
                        if (data[i] == 0x0)
                        {
                            break;
                        }
                        dataAsString.Append(data[i] + " ");
                    }
                    if (data.Count != 0)
                    {
                        string message = Encoding.GetString(data.Slice(0, i));
                        OnMessageReceived(new(message.ToCharArray()));
#if false
                        Debug.WriteLine(dataAsString.ToString());
#endif
#if false
                        Debug.WriteLine(message);
                        // clear buffer
#endif
                        for (i = 0; i < data.Count; i++)
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
                catch (WebSocketException ex)
                {
                    Interlocked.Exchange(ref disconnected, 1);
                    Trace.TraceError("WebSocket exception occured, exiting receive thread.\n" + ex.ToString());
                    throw;
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