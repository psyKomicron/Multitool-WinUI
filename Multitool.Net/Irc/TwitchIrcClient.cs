using Microsoft.UI.Dispatching;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Windows.Foundation;
using Windows.Storage.Streams;

namespace Multitool.Net.Irc
{
    public class TwitchIrcClient : IrcClient
    {
        private static readonly Regex oauthRegex = new(@"^oauth:.+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly string login;

        #region constructors
        public TwitchIrcClient(string login)
        {
            if (oauthRegex.IsMatch(login))
            {
                this.login = login;
            }
            else
            {
                throw new FormatException("Login does not match twitch oauth format");
            }
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
            AssertChannelNameValid(channel);

            Trace.TraceInformation("Trying to join " + channel);
            await Socket.SendAsync(GetBytes($"JOIN #{channel}"), WebSocketMessageType.Text, true, CancellationToken.Token);
            Connected = true;
            Trace.TraceInformation("Joined " + channel);
            //ReceiveThread.Start();
        }

        /// <inheritdoc/>
        public override async Task Part(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);

            await Socket.SendAsync(GetBytes($"PART #{channel}"), WebSocketMessageType.Text, true, CancellationToken.Token);
            Trace.TraceInformation("Left " + channel);
        }

        public override async Task Connect(Uri uri)
        {
            await base.Connect(uri);
            AssertConnectionValid();
            ReceiveThread.Start();

            // send PASS and NICK
            await Socket.SendAsync(GetBytes($"PASS {login}"), WebSocketMessageType.Text, true, CancellationToken.Token);
            await Socket.SendAsync(GetBytes($"NICK {NickName}"), WebSocketMessageType.Text, true, CancellationToken.Token);
        }
        #endregion

        protected override void OnMessageReceived(string message)
        {
            Span<char> chars = new(message.ToCharArray());
            StringBuilder builder = new();
            if (chars[0] == ':')
            {
                int i = 1;
                for (; i < chars.Length; i++)
                {
                    if (chars[i] == '@')
                    {
                        break;
                    }
                }
                builder.Append(chars.Slice(0, i));
            }
            for (int i = 0; i < chars.Length; i++)
            {
                
            }
            InvokeMessageReceived(message);
        }

        #region private methods
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
            CancellationTokenSource cancellationToken = GetCancellationToken();
            System.Timers.Timer t = new(3000);
            t.Elapsed += (s, e) => cancellationToken?.Cancel();

            ArraySegment<byte> buffer = new(new byte[1024]);
            t.Start();
            try
            {
                await Socket.ReceiveAsync(buffer, cancellationToken.Token);
            }
            catch (TaskCanceledException ex) { }
            t.Stop();
            cancellationToken.Dispose();
            cancellationToken = null;
            return GetString(buffer);
        }
        #endregion
    }
}
