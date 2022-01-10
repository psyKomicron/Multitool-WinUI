using Multitool.Net.Twitch.Security;

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Net.Twitch.Irc
{
    public interface ITwitchIrcClient : IAsyncDisposable
    {
        bool AutoLogIn { get; init; }
        TwitchConnectionToken ConnectionToken { get; }
        WebSocketState ClientState { get; }
        Encoding Encoding { get; set; }
        bool IsConnected { get; }
        string NickName { get; set; }
        bool RequestTags { get; init; }
        CancellationTokenSource RootCancellationToken { get; }

        /// <summary>
        /// Fired when the IRC client has connected to a channel.
        /// <para>sender: the client</para>
        /// <para>args: the name of the joined channel</para>
        /// </summary>
        event TypedEventHandler<ITwitchIrcClient, string> Joined;
        /// <summary>
        /// Fired when the IRC client has been disconnected (from an internal error, websocket error or user request).
        /// <para>sender: the client</para>
        /// <para>empty event args</para>
        /// </summary>
        event TypedEventHandler<ITwitchIrcClient, EventArgs> Disconnected;
        /// <summary>
        /// Fired when a message has been received.
        /// <para>sender: the client</para>
        /// <para>the received message</para>
        /// </summary>
        event TypedEventHandler<ITwitchIrcClient, Message> MessageReceived;
        /// <summary>
        /// Fired when the room/channel underwent a change (sub-only mode...)
        /// </summary>
        event TypedEventHandler<ITwitchIrcClient, RoomStates> RoomChanged;

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