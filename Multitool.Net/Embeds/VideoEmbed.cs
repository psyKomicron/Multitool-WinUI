using System;

namespace Multitool.Net.Embeds
{
    public class VideoEmbed : Embed
    {
        public VideoEmbed() : base()
        {
        }

        public DateTime UploadDate { get; internal set; }
        public TimeSpan Duration { get; internal set; }
    }
}
