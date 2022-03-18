using Multitool.Net.Imaging.Json.Bttv;
using Multitool.Net.Irc.Security;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

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

        public override async Task<Emote[]> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            AssertChannelValid(channel);

            string channelId = await helper.GetUserId(channel);
            BttvChannelEmotes jsonData = await GetJsonAsync<BttvChannelEmotes>(string.Format(Resources.BttvChannelEmotesEndpoint, channelId));

            List<Emote> emotes = new();
            if (jsonData != null)
            {
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
            }

            return emotes.ToArray();
        }

        public override Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<Emote[]> FetchGlobalEmotes()
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

            return emotes.ToArray();
        }

        public override Task<List<string>> FetchChannelEmotesIds(string channel)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<Size, string> CreateUrls(string id)
        {
            Dictionary<Size, string> urls = new();
            urls.Add(new(28, 28), string.Format(Resources.BttvEmoteEndpoint, id, "1x"));
            urls.Add(new(56, 56), string.Format(Resources.BttvEmoteEndpoint, id, "2x"));
            urls.Add(new(128, 128), string.Format(Resources.BttvEmoteEndpoint, id, "4x"));
            return urls;
        }
    }
}
