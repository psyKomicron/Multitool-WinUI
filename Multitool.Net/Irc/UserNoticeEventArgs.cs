using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Net.Irc.Twitch
{
    public class UserNoticeEventArgs : EventArgs
    {
        public NoticeType NoticeType { get; internal set; }
        public string SystemMessage { get; internal set; }
        public string Message { get; internal set; }
    }
}
