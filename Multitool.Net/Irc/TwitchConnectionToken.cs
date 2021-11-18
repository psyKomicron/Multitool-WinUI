using System;
using System.Text.RegularExpressions;

namespace Multitool.Net.Irc
{
    public class TwitchConnectionToken : ConnectionToken
    {
        public TwitchConnectionToken(string token) : base(token, new(@"(^oauth:.+)|([0-9A-Za-z-._~+/]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) { }
    }
}
