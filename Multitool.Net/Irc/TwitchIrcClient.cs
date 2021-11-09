using Microsoft.UI.Dispatching;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Windows.Foundation;
using Windows.Storage.Streams;

namespace Multitool.Net.Irc
{
    public class TwitchIrcClient : IrcClient
    {
        private readonly string login;
        //private readonly ArraySegment<byte> buffer = WebSocket.CreateClientBuffer(4096, 4096);

        #region constructors
        public TwitchIrcClient(string login)
        {
            this.login = login;
        }
        #endregion

        #region properties
        //public ConnectionToken ConnectionToken { get; set; }
        #endregion

        #region public methods
        /// <inheritdoc/>
        public override async Task SendMessage(string message)
        {
            AssertConnectionValid();
            

            //Debug.WriteLine($"{message} -> {data}\n");
            Debug.WriteLine(string.Empty);
        }

        /// <inheritdoc/>
        public override async Task Join(string channel)
        {
            AssertConnectionValid();

            await Socket.SendAsync(GetBytes($"JOIN #{channel}"), WebSocketMessageType.Text, true, CancellationToken.Token);
            string data = await ReceiveAsync();
            InvokeMessageReceived(data);
        }

        /// <inheritdoc/>
        public override async Task Part(string channel)
        {
            AssertConnectionValid();
            await SendMessage($"PART #{channel}");
        }

        public override async Task Connect(Uri channel)
        {
            await base.Connect(channel);
            AssertConnectionValid();

            // send PASS and NICK
            await Socket.SendAsync(GetBytes($"PASS {login}"), WebSocketMessageType.Text, true, CancellationToken.Token);
            await Socket.SendAsync(GetBytes($"NICK {NickName}"), WebSocketMessageType.Text, true, CancellationToken.Token);

            string data = await ReceiveAsync();
            InvokeMessageReceived("Client connected : " + data);
        }
        #endregion

        private ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.UTF8.GetBytes(text));
        }

        private string GetString(ArraySegment<byte> buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }

        private async Task<string> ReceiveAsync()
        {
            ArraySegment<byte> buffer = new(new byte[1024]);
            await Socket.ReceiveAsync(buffer, CancellationToken.Token);
            return GetString(buffer);
        }
    }
}
