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
        private static readonly Regex ircJoinCommandRegex = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv JOIN .))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ircMessage = new(@"^:(.+)!\1@\1\.tmi\.twitch\.tv PRIVMSG .+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex joinRegex = new(@"^:(.+)!\1@\1\.tmi\.twitch\.tv JOIN");
        private static readonly Regex namesRegex = new(@"^:(.+)\.tmi\.twitch\.tv 353 \1 = #[a-z0-9]+ :");

        private readonly ConnectionToken login;
        private bool loggedIn;
        private bool hasJoined;
        //private string alias;

        #region constructor
        public TwitchIrcClient(ConnectionToken login) : base(5_000, true)
        {
            // assert login
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            this.login = login;

            Commands.Add(2, namesRegex);
            Commands.Add(1, ircJoinCommandRegex);
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
                else
                {
                    Debug.WriteLine("> message");
                }
            }
        }

        protected override void OnCommandReceived(uint tag, Span<char> command)
        {
            switch (tag)
            {
                case 1:
                    // JOIN
                    int i = 0;
                    for (; i < command.Length; i++)
                    {
                        if (command[i] == '#')
                        {
                            break;
                        }
                    }
                    Trace.TraceInformation($"> Joined {command.Slice(command.Length - i - 1, i - 1).ToString()}");
                    break;
                case 2:
                    // /NAMES
                    Match match = namesRegex.Match(command.ToString());
                    Trace.TraceInformation($"NAMES command: {command.ToString()}");

                    break;
                default:
                    break;
            }
        }

        #region private methods
        private void ParseMessage(Span<char> message)
        {
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

                Span<char> name = message[1..nameLength];

                while (i < message.Length && message[i] != ':')
                {
                    i++;
                }

                Message m = new(message[(i + 1)..].ToString())
                {
                    Author = new(name.ToString())
                };
                InvokeMessageReceived(m);
            }
        }

        private async Task LogIn()
        {
            // send PASS and NICK
#if false
            await Socket.SendAsync(GetBytes($"PASS {login}"), WebSocketMessageType.Text, false, RootCancellationToken.Token);
#else
            await Socket.SendAsync(GetBytes($"PASS {login}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
#endif
            await Socket.SendAsync(GetBytes($"NICK {NickName}"), WebSocketMessageType.Text, true, RootCancellationToken.Token);
            loggedIn = true;
        }
        #endregion
    }
}
