using System.Collections.Generic;

namespace Multitool.Net.Imaging.Json.Ffz
{
#pragma warning disable IDE1006 // Naming Styles
    internal class FfzChannelEmoteData
    {
        public FfzJsonRoom room { get; set; }
        public Dictionary<int, FfzJsonSet> sets { get; set; }
    }
}
