using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch.Irc
{
    internal static class IrcRegexes
    {
        private const string tagRegex = @"^@[a-z\-]+=.*";

        /*static IrcRegices()
        {
            try
            {
                MessageRegex = Regex.Com
            }
            catch { }
        }*/

        public static Regex MessageRegex { get; } = new($@"{tagRegex} :([A-z0-9]+)!\1@\1\.tmi\.twitch\.tv PRIVMSG \#[A-z0-9]+ :.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex JoinRegex { get; } = new(@"^(:[a-z]+![a-z]+@([a-z]+\.tmi.twitch.tv JOIN .))", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex NamesRegex { get; } = new(@"^:(.+)\.tmi\.twitch\.tv 353 \1 = #[a-z0-9]+ :");

        public static Regex PingRegex { get; } = new(@"^PING", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex UserStateRegex { get; } = new($@"{tagRegex} :tmi\.twitch\.tv USERSTATE \#[A-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex RoomStateRegex { get; } = new($@"{tagRegex} :tmi\.twitch\.tv ROOMSTATE \#[A-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex ClearChatRegex { get; } = new($@"{tagRegex} :tmi\.twitch\.tv CLEARCHAT \#[A-z0-9]+ :.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex UserNoticeRegex { get; } = new($@"{tagRegex} :tmi\.twitch\.tv USERNOTICE \#[A-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static Regex NoticeRegex { get; } = new($@"{tagRegex} :tmi\.twitch\.tv NOTICE \#[A-z0-9]+ :.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
