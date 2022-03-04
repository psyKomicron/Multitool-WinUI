using Multitool.Net.Imaging.Json.SevenTV;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }

        public override async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            List<SevenTVJsonEmote> json = await ListEmotesJson(channel);
            return Get7TVEmotes(json, "Channel");
        }

        public override Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<Emote>> FetchGlobalEmotes()
        {
            CheckIfDisposed();
            List<SevenTVJsonEmote> json = await GetJsonAsync<List<SevenTVJsonEmote>>(Resources.SevenTVApiGlobalEmotesEndPoint);
            return Get7TVEmotes(json, "Global");
        }

        public override async Task<List<string>> ListChannelEmotes(string channel)
        {
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

                Dictionary<ImageSize, string> urls = new(); 
                urls.Add(ImageSize.Medium, jsonEmote.urls[0][1]);
                urls.Add(ImageSize.Small, jsonEmote.urls[1][1]);
                urls.Add(ImageSize.Big, jsonEmote.urls[2][1]);

                Emote emote = new(new(jsonEmote.id), jsonEmote.name, urls, jsonEmote.mime)
                {
                    Provider = "7TV",
                    Type = emoteType
                };
                //downloadTasks.Add(DownloadEmoteAsync(emote, new(jsonEmote.urls[0][1]), jsonEmote.mime));
                emotes.Add(emote);
            }

            return emotes;
        }

        private async Task<List<SevenTVJsonEmote>> ListEmotesJson(string channel)
        {
            string url = string.Format(Resources.SevenTVApiChannelEmotesEndPoint, channel);

            using HttpResponseMessage emotesResponse = await Client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
            if (emotesResponse.StatusCode == HttpStatusCode.NotFound)
            {
                Trace.TraceWarning($"#{channel} does not have 7TV emotes (server response 404).");
                return new();
            }
            else
            {
                emotesResponse.EnsureSuccessStatusCode();

                string result = await emotesResponse.Content.ReadAsStringAsync();
                List<SevenTVJsonEmote> json = JsonSerializer.Deserialize<List<SevenTVJsonEmote>>(result);

                return json;
            }
        }
    }
}
