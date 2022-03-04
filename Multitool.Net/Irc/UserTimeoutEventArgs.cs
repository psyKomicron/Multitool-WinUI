using System;

namespace Multitool.Net.Irc.Twitch
{
    public class UserTimeoutEventArgs : EventArgs
    {
        public TimeSpan Timeout { get; internal set; }
        public User User { get; internal set; }
        public string UserName { get; internal set; }
    }
}
