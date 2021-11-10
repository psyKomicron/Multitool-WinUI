
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Irc
{
    public interface IIrcClient : IDisposable
    {
        event TypedEventHandler<IIrcClient, string> MessageReceived;

        string NickName { get; set; }
        WebSocketState ClientState { get; }
        bool Connected { get; }
        CancellationTokenSource CancellationToken { get; }


        /// <summary>
        /// Connects to a IRC server.
        /// <para>
        ///     The <see cref="System.Threading.CancellationToken"/> given to 
        ///     <see cref="ClientWebSocket.ConnectAsync(Uri, System.Threading.CancellationToken)"/> 
        ///     is <see cref="CancellationToken"/>, allowing to cancel all pending operations when 
        ///     the object is closed or disposed.
        /// </para>
        /// </summary>
        /// <param name="channel">IRC server to connect to</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        Task Connect(Uri channel);
        /// <summary>
        /// Connects to an IRC server passing the <paramref name="cancellationToken"/>
        /// to the internal <see cref="WebSocket"/> connect method.
        /// </summary>
        /// <param name="uri">IRC server to connect to</param>
        /// <param name="cancellationToken">To cancel the operation</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        Task Connect(Uri uri, CancellationToken cancellationToken);
        CancellationTokenSource GetCancellationToken();
        Task Join(string channel);
        Task Part(string channel);
        Task SendMessage(string message);
    }
}