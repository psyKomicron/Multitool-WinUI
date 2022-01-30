using System.Collections.Generic;

namespace Multitool.Net.Imaging.Json.Ffz
{
#pragma warning disable IDE1006 // Naming Styles
    internal class FfzJsonData
    {
        public int[] default_sets { get; set; }
        public Dictionary<string, FfzJsonSet> sets { get; set; }
        public object users { get; set; }
    }
}
