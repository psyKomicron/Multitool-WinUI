using Multitool.Net.Imaging.Json.SevenTV;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    /// <summary>
    /// Emote fetcher for the 7TV (SevenTV) API.
    /// </summary>
    public class SevenTVEmoteFetcher : EmoteFetcher
    {
        public SevenTVEmoteFetcher() : base(new())
        {
            Provider = "7TV";
        }

        public override async Task<Emote[]> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            AssertChannelValid(channel);
            List<SevenTVJsonEmote> json = await ListEmotesJson(channel);
            return Get7TVEmotes(json, "Channel").ToArray();
        }

        public override Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            CheckIfDisposed();
            throw new NotImplementedException();
        }

        public override async Task<Emote[]> FetchGlobalEmotes()
        {
            CheckIfDisposed();
            List<SevenTVJsonEmote> json = await GetJsonAsync<List<SevenTVJsonEmote>>(Resources.SevenTVApiGlobalEmotesEndPoint);
            return Get7TVEmotes(json, "Global").ToArray();
        }

        public override async Task<List<string>> FetchChannelEmotesIds(string channel)
        {
            CheckIfDisposed();
            AssertChannelValid(channel);
            List<SevenTVJsonEmote> json = await ListEmotesJson(channel);
            List<string> strings = new(json.Count);
            for (int i = 0; i < json.Count; i++)
            {
                strings.Add(json[i].id);
            }
            return strings;
        }

        private static List<Emote> Get7TVEmotes(List<SevenTVJsonEmote> json, string emoteType)
        {
            List<Emote> emotes = new();

            for (int i = 0; i < json.Count; i++)
            {
                SevenTVJsonEmote jsonEmote = json[i];

                Dictionary<Size, string> urls = new(); 
                urls.Add(new(jsonEmote.width[0], jsonEmote.height[0]), jsonEmote.urls[0][1]);
                urls.Add(new(jsonEmote.width[1], jsonEmote.height[1]), jsonEmote.urls[1][1]);
                urls.Add(new(jsonEmote.width[2], jsonEmote.height[2]), jsonEmote.urls[2][1]);

                Emote emote = new(new(jsonEmote.id), jsonEmote.name, urls, jsonEmote.mime)
                {
                    Provider = "7TV",
                    Type = emoteType
                };
                emotes.Add(emote);
            }

            return emotes;
        }

        private async Task<List<SevenTVJsonEmote>> ListEmotesJson(string channel)
        {
            string url = string.Format(Resources.SevenTVApiChannelEmotesEndPoint, channel);
            List<SevenTVJsonEmote> json = await GetJsonAsync<List<SevenTVJsonEmote>>(url);

            if (json == null)
            {
                Trace.TraceWarning($"#{channel} does not have 7TV emotes (server response 404).");
                return new();
            }
            else
            {
                return json;
            }
        }
    }
}
