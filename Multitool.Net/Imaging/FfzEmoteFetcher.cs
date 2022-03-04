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

            var json = await GetJsonAsync<FfzJsonData>(Resources.FfzApiGlobalEmotesEndPoint);

            List<Emote> emotes = new();
            foreach (KeyValuePair<string, FfzJsonSet> set in json.sets)
            {
                FfzJsonSet jsonEmotes = set.Value;
                foreach (var jsonEmote in jsonEmotes.emoticons)
                {
                    Dictionary<ImageSize, string> urls = new();
                    if (jsonEmote.urls.TryGetValue("1", out string url))
                    {
                        urls.Add(ImageSize.Small, $"https:{url}");
                    }
                    if (jsonEmote.urls.TryGetValue("2", out url))
                    {
                        urls.Add(ImageSize.Medium, $"https:{url}");
                    }
                    if (jsonEmote.urls.TryGetValue("4", out url))
                    {
                        urls.Add(ImageSize.Big, $"https:{url}");
                    }

                    Emote emote = new(new(jsonEmote.id), jsonEmote.name, urls)
                    {
                        Provider = "FFZ",
                        Type = "Global"
                    };
                    emotes.Add(emote);
                }
            }
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
