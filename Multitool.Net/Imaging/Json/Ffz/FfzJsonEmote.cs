using System.Collections.Generic;

namespace Multitool.Net.Imaging.Json.Ffz
{
#pragma warning disable IDE1006 // Naming Styles
    internal class FfzJsonEmote
    {
        public int id { get; set; }
        public string name { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public bool @public { get; set; }
        public bool hidden { get; set; }
        public bool modifier { get; set; }
        public object offset { get; set; }
        public object margins { get; set; }
        public object css { get; set; }
        public FfzJsonOwner owner { get; set; }
        public Dictionary<string, string> urls { get; set; }
        public int status { get; set; }
        public int usage_count { get; set; }
        public string created_at { get; set; }
        public string last_updated { get; set; }
    }
}