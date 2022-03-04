using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Net.Imaging.Json.Bttv
{
#pragma warning disable IDE1006 // Naming Styles for json deserialization
    internal class BttvChannelEmotes
    {
        public string id { get; set; }
        public List<string> bots { get; set; }
        public string avatar { get; set; }
        public List<BttvJsonEmote> channelEmotes { get; set; }
        public List<BttvJsonSharedEmote> sharedEmotes { get; set; }
    }
}
