using Multitool.Net.Twitch.Json;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
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

        public EmoteFetcher() { }

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

        public async Task<List<Emote>> GetGlobalEmotes()
        {
            CheckIfDisposed();
            CheckConnectionToken();

            using HttpResponseMessage emotesResponse = await client.GetAsync(new(Properties.Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();

#if true
            string s = await emotesResponse.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonData>(s);
#else
            using DataReader jsonReader = new(await emotesResponse.Content.ReadAsInputStreamAsync());
            JsonData data = await JsonSerializer.DeserializeAsync<JsonData>((await emotesResponse.Content.ReadAsInputStreamAsync()).AsStreamForRead());
#endif

            List<JsonEmote> list = data.data;
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();

            for (int i = 0; i < list.Count; i++)
            {
                JsonEmote jsonEmote = list[i];
                Emote emote = new(new(jsonEmote.id), jsonEmote.name);

                downloadTasks.Add(DownloadEmote(emote, jsonEmote));

                emotes.Add(emote);
            }

            await Task.WhenAll(downloadTasks);

            return emotes;
        }

        public async Task<Emote> GetChannelEmotes(string emoteName)
        {
            throw new NotImplementedException();
        }

        private async Task DownloadEmote(Emote emote, JsonEmote jsonEmote)
        {
            string emoteUrl = DefaultImageSize switch
            {
                ImageSize.Small => jsonEmote.images.url_1x,
                ImageSize.Medium => jsonEmote.images.url_2x,
                ImageSize.Big => jsonEmote.images.url_4x,
                _ => jsonEmote.images.url_2x,
            };

            using HttpResponseMessage reponse = await client.GetAsync(new(emoteUrl), HttpCompletionOption.ResponseHeadersRead).AsTask();
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

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void CheckConnectionToken()
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
    }
}
