using Multitool.Net.Imaging.Json;
using Multitool.Net.Imaging.Json.Ffz;
using Multitool.Net.Imaging.Json.SevenTV;
using Multitool.Net.Properties;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Twitch
{
    /// <summary>
    /// Gets emotes from Twitch's API.
    /// </summary>
    public class EmoteFetcher : IDisposable
    {
        private readonly HttpClient client = new();
        private bool disposed;
        private TwitchConnectionToken connectionToken;

        public EmoteFetcher(TwitchConnectionToken connectionToken)
        {
            ConnectionToken = connectionToken;
            client.DefaultRequestHeaders.Authorization = new("Bearer", ConnectionToken.Token);
            client.DefaultRequestHeaders.Add(new("Client-Id", ConnectionToken.ClientId));
        }

        public ImageSize DefaultImageSize { get; set; } = ImageSize.Medium;

        public TwitchConnectionToken ConnectionToken
        {
            get => connectionToken;
            set
            {
                connectionToken = value;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Authorization = new("Bearer", connectionToken.Token);
                client.DefaultRequestHeaders.Add(new("Client-Id", connectionToken.ClientId));
            }
        }

        public void Dispose()
        {
            client.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }

        public async Task<List<Emote>> GetGlobalTwitchEmotes()
        {
            CheckIfDisposed();

            Trace.TraceInformation("Downloading global emotes...");

            using HttpResponseMessage emotesResponse = await client.GetAsync(new(Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();

#if true
            string s = await emotesResponse.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonData>(s);
#else
            using DataReader jsonReader = new(await emotesResponse.Content.ReadAsInputStreamAsync());
            JsonData data = await JsonSerializer.DeserializeAsync<JsonData>((await emotesResponse.Content.ReadAsInputStreamAsync()).AsStreamForRead());
#endif
            return await DownloadTwitchEmotesAsync(data);
        }

        public async Task<List<Emote>> GetGlobal7TVEmotes()
        {
            CheckIfDisposed();

            using HttpResponseMessage httpResponse = await client.GetAsync(new(Resources.SevenTVApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            httpResponse.EnsureSuccessStatusCode();
            
            string s = await httpResponse.Content.ReadAsStringAsync();
            List<SevenTVJsonEmote> json = JsonSerializer.Deserialize<List<SevenTVJsonEmote>>(s);

            return await Download7TVEmotesAsync(json);
        }

        public async Task<List<Emote>> GetGlobalFfzEmotes()
        {
            CheckIfDisposed();

            using HttpResponseMessage httpResponse = await client.GetAsync(new(Resources.FfzApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
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
                    downloadTasks.Add(DownloadEmoteAsync(emote, new($"https:{jsonEmote.urls["1"]}")));
                    emotes.Add(emote);
                }
            }

            await Task.WhenAll(downloadTasks);

            return emotes;
        }

        public async Task<List<Emote>> GetTwitchChannelEmotes(string channelId)
        {
            CheckIfDisposed();

            string id = string.Empty;

            using HttpResponseMessage getUsersEndpointResponse = await client.GetAsync(new(string.Format(Resources.TwitchApiGetUsersByLoginEndpoint, channelId)), HttpCompletionOption.ResponseHeadersRead);
            getUsersEndpointResponse.EnsureSuccessStatusCode();

            string s = await getUsersEndpointResponse.Content.ReadAsStringAsync();
            JsonDocument idData = JsonDocument.Parse(s);

            if (idData.RootElement.TryGetProperty("data", out JsonElement jsonData))
            {
                if (jsonData.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("\"jsonData\" is not an array");
                }
                
                if (jsonData[0].TryGetProperty("id", out JsonElement value))
                {
                    id = value.ToString();
                    string url = string.Format(Resources.TwitchApiChannelEmoteEndPoint, id);

                    using HttpResponseMessage emotesResponse = await client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
                    emotesResponse.EnsureSuccessStatusCode();

                    s = await emotesResponse.Content.ReadAsStringAsync();
                    JsonData data = JsonSerializer.Deserialize<JsonData>(s);

                    return await DownloadTwitchEmotesAsync(data);
                }
                else
                {
                    Exception ex = new InvalidOperationException("Unable to parse user id from Twitch API GetUsers endpoint");
                    ex.Data.Add("Full response", s);
                    throw ex;
                }
            }
            else
            {
                Exception ex = new InvalidOperationException("Unable to parse Twitch API GetUsers endpoint response. (does not have { data: {...} })");
                ex.Data.Add("Full response", s);
                throw ex;
            }
        }

        public async Task<List<Emote>> GetChannel7TVEmotes(string channel)
        {
            CheckIfDisposed();

            string url = string.Format(Resources.SevenTVApiChannelEmotesEndPoint, channel);
            using HttpResponseMessage emotesResponse = await client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();

            List<SevenTVJsonEmote> json = JsonSerializer.Deserialize<List<SevenTVJsonEmote>>(await emotesResponse.Content.ReadAsStringAsync());

            return await Download7TVEmotesAsync(json);
        }

        #region private members
        private async Task<List<Emote>> DownloadTwitchEmotesAsync(JsonData data)
        {
            List<JsonEmote> list = data.data;
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            for (int i = 0; i < list.Count; i++)
            {
                JsonEmote jsonEmote = list[i];
                Emote emote = new(new(jsonEmote.id), jsonEmote.name);
                emote.Provider = "Global emote";
                string emoteUrl = DefaultImageSize switch
                {
                    ImageSize.Small => jsonEmote.images.url_1x,
                    ImageSize.Medium => jsonEmote.images.url_2x,
                    ImageSize.Big => jsonEmote.images.url_4x,
                    _ => jsonEmote.images.url_2x,
                };
                downloadTasks.Add(DownloadEmoteAsync(emote, new(emoteUrl)));

                emotes.Add(emote);
            }

            await Task.WhenAll(downloadTasks);
            return emotes;
        }

        private async Task<List<Emote>> Download7TVEmotesAsync(List<SevenTVJsonEmote> json)
        {
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            for (int i = 0; i < json.Count; i++)
            {
                SevenTVJsonEmote jsonEmote = json[i];
                Emote emote = new(new(jsonEmote.id), jsonEmote.name);
                emote.Provider = "7TV Global emote";
                downloadTasks.Add(DownloadEmoteAsync(emote, new(jsonEmote.urls[0][1])));
                emotes.Add(emote);
            }

            await Task.WhenAll(downloadTasks);
            return emotes;
        }

        private async Task DownloadEmoteAsync(Emote emote, Uri emoteUrl)
        {
            try
            {
                using HttpResponseMessage reponse = await client.GetAsync(emoteUrl, HttpCompletionOption.ResponseHeadersRead).AsTask();
                if (reponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    Trace.TraceError($"Bad request:\n{await reponse.Content.ReadAsStringAsync()}\n{reponse.Headers}");
                }
                reponse.EnsureSuccessStatusCode();

                IBuffer stream = await reponse.Content.ReadAsBufferAsync();
                using DataReader dataReader = DataReader.FromBuffer(stream);

                byte[] bytes = new byte[dataReader.UnconsumedBufferLength];
                int pos = 0;
                while (dataReader.UnconsumedBufferLength > 0)
                {
                    if (pos >= bytes.Length)
                    {
                        Trace.TraceWarning("Index out of bounds");
                        break;
                    }
                    bytes[pos] = dataReader.ReadByte();
                    pos++;
                }

                await emote.SetImage(bytes);
            }
            catch
            {
                Trace.TraceError($"emote name: {emote.Name}, uri: {emoteUrl.OriginalString}");
                throw;
            }
        }

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
        #endregion
    }
}
