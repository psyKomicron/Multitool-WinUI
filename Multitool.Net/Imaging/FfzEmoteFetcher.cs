using Multitool.Net.Imaging.Json.Ffz;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    /// <summary>
    /// Emote fetcher for the FrankerFaceZ API. Not yet implemented.
    /// </summary>
    public class FfzEmoteFetcher : EmoteFetcher
    {
        public FfzEmoteFetcher() : base(new())
        {
        }

        public override async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
#if DEBUG
            return await Task.Run(() => new List<Emote>());
#else
            throw new NotImplementedException();
#endif
        }

        public override Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<Emote>> FetchGlobalEmotes()
        {
            CheckIfDisposed();

            using HttpResponseMessage httpResponse = await Client.GetAsync(new(Resources.FfzApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            httpResponse.EnsureSuccessStatusCode();

            string s = await httpResponse.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<FfzJsonData>(s);

            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            foreach (KeyValuePair<string, FfzJsonSet> set in json.sets)
            {
                FfzJsonSet jsonEmotes = set.Value;
                foreach (var jsonEmote in jsonEmotes.emoticons)
                {
                    Emote emote = new(new(jsonEmote.id), jsonEmote.name);
                    emote.Provider = "FFZ Global emote";
                    downloadTasks.Add(DownloadEmoteAsync(emote, new($"https:{jsonEmote.urls["1"]}"), string.Empty));
                    emotes.Add(emote);
                }
            }

            await Task.WhenAll(downloadTasks);

            return emotes;
        }

        public override async Task<List<string>> ListChannelEmotes(string channel)
        {
#if DEBUG
            return await Task.Run(() => new List<string>());
#else
            throw new NotImplementedException();
#endif
        }
    }
}
