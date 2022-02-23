using System;

namespace Multitool.Net.Embeds
{
    public class YoutubeEmbed : VideoEmbed
    {
        public YoutubeEmbed()
        {
        }

        public string Author { get; internal set; }
        public string ChannelId { get; internal set; }
        public DateTime PublishDate { get; internal set; }
        public bool FamilyFriendly { get; internal set; }
        public string Genre { get; internal set; }
        public long Interactions { get; internal set; }
        public bool Paid { get; internal set; }
        public YoutubeRegions Regions { get; }
        public Uri EmbedUrl { get; internal set; }
        public bool Unlisted { get; internal set; }
        public string VideoId { get; internal set; }
    }
}
