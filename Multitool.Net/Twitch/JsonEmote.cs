namespace Multitool.Net.Twitch.Json
{
    internal struct JsonEmote
    {
        public string id;
        public string name;
        public JsonImages images;
        public string[] format;
        public string[] scale;
        public string[] theme_mode;
    }

    internal struct JsonImages
    {
        public string url_1x;
        public string url_2x;
        public string url_4x;
    }
}
