using System.Collections.Generic;

namespace Multitool.Net.Imaging.Json.Ffz
{
#pragma warning disable IDE1006 // Naming Styles
    internal class FfzJsonSet
    {
        public int id { get; set; }
        public int _type { get; set; }
        public object icon { get; set; }
        public string title { get; set; }
        public object css { get; set; }
        public List<FfzJsonEmote> emoticons { get; set; }
    }
}