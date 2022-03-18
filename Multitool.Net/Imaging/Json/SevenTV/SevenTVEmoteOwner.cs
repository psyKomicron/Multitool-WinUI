namespace Multitool.Net.Imaging.Json.SevenTV
{
#pragma warning disable IDE1006 // Naming Styles for json deserialization
    internal class SevenTVEmoteOwner
    {
        public string id { get; set; }
        public string twitch_id { get; set; }
        public string login { get; set; }
        public string display_name { get; set; }
        public SevenTVJsonRole role { get; set; }
    }
}
