using Multitool.Net.Imaging.Json.SevenTV;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
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
            return await Download7TVEmotesAsync(json);
        }

        public override Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<Emote>> FetchGlobalEmotes()
        {
            CheckIfDisposed();

            using HttpResponseMessage httpResponse = await Client.GetAsync(new(Resources.SevenTVApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            httpResponse.EnsureSuccessStatusCode();

            string s = await httpResponse.Content.ReadAsStringAsync();
            List<SevenTVJsonEmote> json = JsonSerializer.Deserialize<List<SevenTVJsonEmote>>(s);

            return await Download7TVEmotesAsync(json);
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

        private async Task<List<Emote>> Download7TVEmotesAsync(List<SevenTVJsonEmote> json)
        {
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            for (int i = 0; i < json.Count; i++)
            {
                SevenTVJsonEmote jsonEmote = json[i];
                Emote emote = new(new(jsonEmote.id), jsonEmote.name);
                emote.Provider = "7TV";
                downloadTasks.Add(DownloadEmoteAsync(emote, new(jsonEmote.urls[0][1]), jsonEmote.mime));
                emotes.Add(emote);
            }

            await Task.WhenAll(downloadTasks);
            return emotes;
        }

        private async Task<List<SevenTVJsonEmote>> ListEmotesJson(string channel)
        {
            string url = string.Format(Resources.SevenTVApiChannelEmotesEndPoint, channel);
            using HttpResponseMessage emotesResponse = await Client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();
            string result = await emotesResponse.Content.ReadAsStringAsync();
            List<SevenTVJsonEmote> json = JsonSerializer.Deserialize<List<SevenTVJsonEmote>>(result);
            return json;
        }
    }
}
