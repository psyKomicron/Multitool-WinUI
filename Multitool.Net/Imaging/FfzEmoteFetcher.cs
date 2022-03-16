using Multitool.Net.Imaging.Json.Ffz;
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
    /// Emote fetcher for the FrankerFaceZ API. Partially implemented.
    /// </summary>
    public class FfzEmoteFetcher : EmoteFetcher
    {
        public FfzEmoteFetcher() : base(new())
        {
            Provider = "FrankerFaceZ";
        }

        public override async Task<Emote[]> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            AssertChannelValid(channel);

            var json = await GetJsonAsync<FfzChannelEmoteData>(string.Format(Resources.FfzApiServerUrl, $"room/{channel}"));
            if (json != null)
            {
                string type = $"Channel ({json.room})";
                return CreateEmotes(type, json.sets).ToArray(); 
            }
            else
            {
                return Array.Empty<Emote>();
            }
        }

        public override Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            CheckIfDisposed();
            AssertChannelValid(channel);

            throw new NotImplementedException();
        }

        public override async Task<Emote[]> FetchGlobalEmotes()
        {
            CheckIfDisposed();
            
            var json = await GetJsonAsync<FfzGlobalJsonData>(string.Format(Resources.FfzApiServerUrl, "set/global"));
            if (json != null)
            {
                return CreateEmotes("Global", json.sets).ToArray();
            }
            else
            {
                return Array.Empty<Emote>();
            }
        }

        public override async Task<List<string>> FetchChannelEmotesIds(string channel)
        {
#if DEBUG
            return await Task.Run(() => new List<string>());
#else
            throw new NotImplementedException();
#endif
        }

        private List<Emote> CreateEmotes<TKey>(string emoteType, Dictionary<TKey, FfzJsonSet> sets)
        {
            List<Emote> emotes = new();
            foreach (KeyValuePair<TKey, FfzJsonSet> set in sets)
            {
                FfzJsonSet jsonEmotes = set.Value;
                foreach (var jsonEmote in jsonEmotes.emoticons)
                {
                    Dictionary<Size, string> urls = new();
                    if (jsonEmote.urls.TryGetValue("1", out string url))
                    {
                        urls.Add(new(jsonEmote.width, jsonEmote.height), $"https:{url}");
                    }
                    if (jsonEmote.urls.TryGetValue("2", out url))
                    {
                        urls.Add(new(jsonEmote.width * 2, jsonEmote.height * 2), $"https:{url}");
                    }
                    if (jsonEmote.urls.TryGetValue("4", out url))
                    {
                        urls.Add(new(jsonEmote.width * 4, jsonEmote.height * 4), $"https:{url}");
                    }

                    Emote emote = new(jsonEmote.id.ToString(), jsonEmote.name, urls)
                    {
                        Provider = Provider,
                        Type = emoteType
                    };
                    emotes.Add(emote);
                }
            }
            return emotes;
        }
    }
}
