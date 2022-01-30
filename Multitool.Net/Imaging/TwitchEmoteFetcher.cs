using Multitool.Net.Imaging.Json;
using Multitool.Net.Properties;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    /// <summary>
    /// Gets emotes from Twitch's API.
    /// </summary>
    public class TwitchEmoteFetcher : EmoteFetcher
    {
        private TwitchConnectionToken connectionToken;

        public TwitchEmoteFetcher(TwitchConnectionToken connectionToken) : base(new())
        {
            ConnectionToken = connectionToken;
            Client.DefaultRequestHeaders.Authorization = new("Bearer", ConnectionToken.Token);
            Client.DefaultRequestHeaders.Add(new("Client-Id", ConnectionToken.ClientId));
        }

        public TwitchConnectionToken ConnectionToken
        {
            get => connectionToken;
            set
            {
                connectionToken = value;
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Authorization = new("Bearer", connectionToken.Token);
                Client.DefaultRequestHeaders.Add(new("Client-Id", connectionToken.ClientId));
            }
        }

        public override async Task<List<Emote>> FetchGlobalEmotes()
        {
            CheckIfDisposed();
            CheckToken();

            using HttpResponseMessage emotesResponse = await Client.GetAsync(new(Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();

#if true
            string s = await emotesResponse.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<TwitchJsonData>(s);
#else
            using DataReader jsonReader = new(await emotesResponse.Content.ReadAsInputStreamAsync());
            JsonData data = await JsonSerializer.DeserializeAsync<JsonData>((await emotesResponse.Content.ReadAsInputStreamAsync()).AsStreamForRead());
#endif
            return await DownloadTwitchEmotes(data);
        }

        public override async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            CheckToken();

            return await DownloadTwitchEmotes(await GetJsonDataForChannel(channel));
        }

        public override Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public override async Task<List<string>> ListChannelEmotes(string channel)
        {
            TwitchJsonData data = await GetJsonDataForChannel(channel);
            List<string> strings = new();
            var emotes = data.data;
            for (int i = 0; i < emotes.Count; i++)
            {
                strings.Add(emotes[i].id);
            }
            return strings;
        }

        #region private members
        private async Task<TwitchJsonData> GetJsonDataForChannel(string channel)
        {
            using HttpResponseMessage getUsersEndpointResponse = await Client.GetAsync(new(string.Format(Resources.TwitchApiGetUsersByLoginEndpoint, channel)), HttpCompletionOption.ResponseHeadersRead);
            getUsersEndpointResponse.EnsureSuccessStatusCode();

            string s = await getUsersEndpointResponse.Content.ReadAsStringAsync();
            JsonDocument idData = JsonDocument.Parse(s);

            if (idData.RootElement.TryGetProperty("data", out JsonElement jsonData))
            {
                if (jsonData.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"Twitch api 'channel emotes' did not reply with a correct data type. Expected array, got {jsonData.ValueKind}");
                }

                if (jsonData[0].TryGetProperty("id", out JsonElement value))
                {
                    string url = string.Format(Resources.TwitchApiChannelEmoteEndPoint, value.ToString());
                    using HttpResponseMessage emotesResponse = await Client.GetAsync(new(url), HttpCompletionOption.ResponseHeadersRead);
                    emotesResponse.EnsureSuccessStatusCode();

                    s = await emotesResponse.Content.ReadAsStringAsync();
                    TwitchJsonData data = JsonSerializer.Deserialize<TwitchJsonData>(s);
                    return data;
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

        private async Task<List<Emote>> DownloadTwitchEmotes(TwitchJsonData data)
        {
            List<TwitchJsonEmote> list = data.data;
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            for (int i = 0; i < list.Count; i++)
            {
                TwitchJsonEmote jsonEmote = list[i];
                Emote emote = new(new(jsonEmote.id), jsonEmote.name);
                emote.Provider = "Twitch emote";
                string emoteUrl = DefaultImageSize switch
                {
                    ImageSize.Small => jsonEmote.images.url_1x,
                    ImageSize.Medium => jsonEmote.images.url_2x,
                    ImageSize.Big => jsonEmote.images.url_4x,
                    _ => jsonEmote.images.url_2x,
                };
                downloadTasks.Add(DownloadEmoteAsync(emote, new(emoteUrl), string.Empty));

                emotes.Add(emote);
            }

            await Task.WhenAll(downloadTasks);
            return emotes;
        }

        private void CheckToken()
        {
            if (ConnectionToken is null)
            {
                throw new ArgumentNullException($"{nameof(ConnectionToken)} is null.{nameof(EmoteFetcher)} cannot make calls to the Twitch API without a connection token.");
            }
            if (!ConnectionToken.Validated)
            {
                throw new ArgumentException("Token has not been validated. Call the appropriate method to validate the token.");
            }
            if (ConnectionToken.ClientId is null)
            {
                throw new ArgumentNullException("Client id is null.");
            }
        }
        #endregion
    }
}
