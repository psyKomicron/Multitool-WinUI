using System;
namespace Multitool.Net.Embeds
{
    public class Embed
    {
        public Embed()
        {
        }

        public string Title { get; internal set; }
        public Uri ThumbnailUrl { get; internal set; }
        public Uri Url { get; internal set; }
        public string Description { get; internal set; }
    }
}