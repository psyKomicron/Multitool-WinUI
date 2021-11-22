
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Irc
{
    public interface IIrcClient : IAsyncDisposable
    {
        event TypedEventHandler<IIrcClient, Message> MessageReceived;

        string NickName { get; set; }
        WebSocketState ClientState { get; }
        bool Connected { get; }
        CancellationTokenSource RootCancellationToken { get; }
        Encoding Encoding { get; set; }

        /// <summary>
        /// Connects to a IRC server.
        /// <para>
        ///     The <see cref="System.Threading.CancellationToken"/> given to 
        ///     <see cref="ClientWebSocket.ConnectAsync(Uri, System.Threading.CancellationToken)"/> 
        ///     is <see cref="RootCancellationToken"/>, allowing to cancel all pending operations when 
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
        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation</returns>
        Task Disconnect();
        CancellationTokenSource GetCancellationToken();
        /// <summary>
        /// Joins a IRC channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the socket is not open (<see cref="WebSocketState.Open"/>)</exception>
        /// <exception cref="ArgumentException">Thrown when the socket state is not recognized (default clause in the switch)</exception>
        /// <exception cref="ArgumentException">Thrown if the channel name is not valid (will carry the value of <paramref name="channel"/>)</exception>
        Task Join(string channel);
        /// <summary>
        /// Leaves <paramref name="channel"/>.
        /// </summary>
        /// <param name="channel">Name of the channel to leave</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        Task Part(string channel);
        /// <summary>
        /// Sends a message through the IRC channel.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <returns>The task object representing the asynchronous operation</returns>
        Task SendMessage(string message);
    }
}