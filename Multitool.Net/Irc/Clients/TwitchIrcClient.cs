
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Multitool.Net.Irc
{
    public class TwitchIrcClient : IrcClient
    {
        // commands
        private static readonly Regex joinCommandRegex = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv JOIN .))", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex namesRegex = new(@"^:(.+)\.tmi\.twitch\.tv 353 \1 = #[a-z0-9]+ :");
        private static readonly Regex pingRegex = new(@"^PING", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex messageRegex = new(@"^:(.+)!\1@\1\.tmi\.twitch\.tv PRIVMSG .+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ConnectionToken login;
        private readonly UserFactory factory = new();
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

            Commands.Add(1, joinCommandRegex);
            Commands.Add(2, namesRegex);
            Commands.Add(3, pingRegex);
        }
        #endregion

        #region properties
        public bool AutoConnect { get; init; }
        #endregion

        #region public methods
        /// <inheritdoc/>
        public override async Task SendMessage(string message)
        {
            throw new NotImplementedException();
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
            if (hasJoined)
            {
                if (messageRegex.IsMatch(message.ToString()))
                {
                    ParseMessage(message);
                }
                else
                {
                    Debug.WriteLine("> message : " + message.ToString());
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
                case 3:
                    Trace.TraceInformation("Pong-ing twitch");
                    command[1] = 'O';
                    Socket.SendAsync(GetBytes(command), WebSocketMessageType.Text, true, RootCancellationToken.Token);
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
                    Author = factory.GetUser(name.ToString(), message)
                };
                InvokeMessageReceived(m);
            }
        }

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
