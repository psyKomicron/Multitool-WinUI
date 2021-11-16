using Microsoft.UI.Dispatching;

using Multitool.Optimisation;

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
        private static readonly Regex ircCommandRegex = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv [A-Z]+))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ircMessage = new(@"^:(.+)!\1@\1\.tmi\.twitch\.tv PRIVMSG", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string login;
        private readonly CircularBag<string> history = new(5000);
        private bool loggedIn;
        private bool hasJoined;

        #region constructor
        public TwitchIrcClient(string login) : base(5_000, true)
        {
            Regex oauthRegex = new(@"^oauth:.+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
        public bool AutoConnect { get; init; }
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
            if (!loggedIn)
            {
                ReceiveThread.Start();
                await LogIn();
            }
    
            Trace.TraceInformation("Trying to join " + channel);
            await Socket.SendAsync(GetBytes($"JOIN #{channel}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            Trace.TraceInformation("> Joined " + channel);
            hasJoined = true;
        }

        /// <inheritdoc/>
        public override async Task Part(string channel)
        {
            AssertConnectionValid();
            AssertChannelNameValid(channel);
            if (hasJoined)
            {
                await Socket.SendAsync(GetBytes($"PART #{channel}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
                hasJoined = false;
                Trace.TraceInformation("> Left " + channel);
            }
            else
            {
                Trace.TraceInformation("> Already left channel");
            }
        }

        public override async Task Connect(Uri uri)
        {
            await base.Connect(uri);
            AssertConnectionValid();

            if (AutoConnect)
            {
                await LogIn();
                ReceiveThread.Start();
            }
        }
        #endregion

        protected override void OnMessageReceived(Span<char> message)
        {
            if (message.StartsWith("PING"))
            {
                Trace.TraceInformation("Pong-ing twitch");
                message[1] = 'O';
                Socket.SendAsync(GetBytes(message), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            }
            else if (hasJoined)
            {
                if (ircMessage.IsMatch(message.ToString()))
                {
                    ParseMessage(message);
                }
                else if (ircCommandRegex.IsMatch(message.ToString()))
                {
                    Trace.TraceInformation(message.ToString());
                }
            }
        }

        #region private methods
        private void ParseMessage(Span<char> message)
        {
            StringBuilder builder = new();
            int i = 1;
            if (message[0] == ':')
            {
                int nameLength = 0;
                for (; i < message.Length; i++)
                {
                    if (message[i] == '@')
                    {
                        break;
                    }
                    else if (message[i] == '!')
                    {
                        nameLength = i;
                    }
                }
                Span<char> name = message.Slice(1, nameLength);
                if (name.ToString() == "psykomicron!")
                {
                    Trace.TraceError(".");
                }
                builder.Append(name).Append(' ').Append(':').Append(' ');

                while (i < message.Length && message[i] != ':')
                {
                    i++;
                }
                builder.Append(message.Slice(i + 1));
                string s = builder.ToString();
                history.Add(s);
                InvokeMessageReceived(s);
            }
        }

        private ArraySegment<byte> GetBytes(string text)
        {
            return new(Encoding.GetBytes(text));
        }

        private ArraySegment<byte> GetBytes(Span<char> text)
        {
            return new(Encoding.GetBytes(text.ToArray()));
        }

        /*private async Task<string> ReceiveAsync()
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
        }*/

        private async Task LogIn()
        {
            // send PASS and NICK
            await Socket.SendAsync(GetBytes($"PASS {login}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            await Socket.SendAsync(GetBytes($"NICK {NickName}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            loggedIn = true;
        }
        #endregion
    }
}
