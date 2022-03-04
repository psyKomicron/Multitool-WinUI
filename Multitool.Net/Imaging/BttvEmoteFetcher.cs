using Multitool.Net.Imaging.Json.Bttv;
using Multitool.Net.Irc.Security;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    public class BttvEmoteFetcher : EmoteFetcher
    {
        private readonly TwitchApiHelper helper;

        public BttvEmoteFetcher(TwitchConnectionToken connectionToken) : base(new())
        {
            Provider = "BetterTTV";
            helper = new(connectionToken);
        }

        public override async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();

            string channelId = await helper.GetUserId(channel);
            BttvChannelEmotes jsonData = await GetJsonAsync<BttvChannelEmotes>(string.Format(Resources.BttvChannelEmotesEndpoint, channelId));

            List<Emote> emotes = new();
            for (int i = 0; i < jsonData.channelEmotes.Count; i++)
            {
                Emote emote = new(new(jsonData.channelEmotes[i].id), jsonData.channelEmotes[i].code, CreateUrls(jsonData.channelEmotes[i].id), jsonData.channelEmotes[i].imageType)
                {
                    CreatorId = jsonData.channelEmotes[i].userId,
                    Provider = Provider,
                    Type = "Channel"
                };
                emotes.Add(emote);
            }

            for (int i = 0; i < jsonData.sharedEmotes.Count; i++)
            {
                Emote emote = new(new(jsonData.sharedEmotes[i].id), jsonData.sharedEmotes[i].code, CreateUrls(jsonData.sharedEmotes[i].id), jsonData.sharedEmotes[i].imageType)
                {
                    CreatorId = jsonData.sharedEmotes[i].user.displayName,
                    Provider = Provider,
                    Type = "Shared"
                };
                emotes.Add(emote);
            }

            return emotes;
        }

        public override Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<Emote>> FetchGlobalEmotes()
        {
            CheckIfDisposed();

            var jsonEmotes = await GetJsonAsync<List<BttvJsonEmote>>(Resources.BttvGlobalEmotesEndpoint);
            List<Emote> emotes = new();
            for (int i = 0; i < jsonEmotes.Count; i++)
            {
                Emote emote = new(new(jsonEmotes[i].id), jsonEmotes[i].code, CreateUrls(jsonEmotes[i].id), jsonEmotes[i].imageType)
                {
                    CreatorId = jsonEmotes[i].userId,
                    Provider = Provider,
                    Type = "Global"
                };
                emotes.Add(emote);
            }

            return emotes;
        }

        public override Task<List<string>> ListChannelEmotes(string channel)
        {
            throw new NotImplementedException();
        }

        private Dictionary<ImageSize, string> CreateUrls(string id)
        {
            Dictionary<ImageSize, string> urls = new();
            urls.Add(ImageSize.Small, string.Format(Resources.BttvEmoteEndpoint, id, "1x"));
            urls.Add(ImageSize.Medium, string.Format(Resources.BttvEmoteEndpoint, id, "2x"));
            urls.Add(ImageSize.Big, string.Format(Resources.BttvEmoteEndpoint, id, "4x"));
            return urls;
        }
    }
}
