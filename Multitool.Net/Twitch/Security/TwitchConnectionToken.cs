using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch.Security
{
    public class TwitchConnectionToken : ConnectionToken
    {
        public TwitchConnectionToken(string token) : base(token, new(@"^([0-9A-Za-z-._~+/]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) { }

        public override string ToString()
        {
            return "oauth:" + base.ToString();
        }
    }
}
