namespace Multitool.Net.Imaging.Json.SevenTV
{
#pragma warning disable IDE1006 // Naming Styles
    internal class SevenTVJsonData
    {
        public SevenTVJsonEmote[] data { get; set; }
    }

    internal class SevenTVJsonEmote
    {
        public string id { get; set; }
        public string name { get; set; }
        public SevenTVEmoteOwner owner { get; set; }
        public int visibility { get; set; }
        public string[] visibility_simple { get; set; }
        public string mime { get; set; }
        public int status { get; set; }
        public object[] tags { get; set; }
        public int[] width { get; set; }
        public int[] height { get; set; }
        public string[][] urls { get; set; }
    }
}
