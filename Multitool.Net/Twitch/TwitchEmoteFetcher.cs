
using Multitool.Net.Twitch.Json;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Twitch
{
    /// <summary>
    /// Gets emotes from Twitch's API
    /// </summary>
    public class TwitchEmoteFetcher : IDisposable
    {
        private readonly HttpClient client = new();
        private bool disposed;
        private readonly TwitchConnectionToken token;

        public TwitchEmoteFetcher(TwitchConnectionToken connectionToken)
        {
            token = connectionToken;
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
            client.DefaultRequestHeaders.Add(new("Client-Id", token.ClientId));
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
            if (!token.Validated)
            {
                throw new ArgumentException("Token has not been validated");
            }
            if (token.ClientId is null)
            {
                throw new ArgumentNullException("Client id is null");
            }

            using HttpResponseMessage emotesResponse = await client.GetAsync(new(Properties.Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
            emotesResponse.EnsureSuccessStatusCode();

#if true
            string s = await emotesResponse.Content.ReadAsStringAsync();
            Debug.WriteLine(s[..100]);
            var data = JsonSerializer.Deserialize<JsonData>(s);
#else
            using DataReader jsonReader = new(await emotesResponse.Content.ReadAsInputStreamAsync());
            JsonData data = await JsonSerializer.DeserializeAsync<JsonData>((await emotesResponse.Content.ReadAsInputStreamAsync()).AsStreamForRead());
#endif

            List<JsonEmote> list = data.data;
            List<Emote> emotes = new();
            List<Task> downloadTasks = new();
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < list.Count; i++)
            {
                JsonEmote jsonEmote = list[i];
                Emote emote = new();
                emote.Id = new(jsonEmote.id);
                emote.Name = jsonEmote.name;
                downloadTasks.Add(DownloadEmote(emote, jsonEmote));
                emotes.Add(emote);
            }
            await Task.WhenAll(downloadTasks);
            sw.Stop();
            Trace.TraceInformation($"Downloaded {emotes.Count} emotes in {sw.Elapsed}");
            return emotes;
        }

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private async Task DownloadEmote(Emote emote, JsonEmote jsonEmote)
        {
            // TODO : Parallelisation can be better -> download and reponse reading can be asynchronous
#if DEBUG
            Task<HttpResponseMessage>[] downloadTasks = new Task<HttpResponseMessage>[3];
            downloadTasks[0] = client.GetAsync(new(jsonEmote.images.url_1x), HttpCompletionOption.ResponseHeadersRead).AsTask();
            downloadTasks[1] = client.GetAsync(new(jsonEmote.images.url_2x), HttpCompletionOption.ResponseHeadersRead).AsTask();
            downloadTasks[2] = client.GetAsync(new(jsonEmote.images.url_4x), HttpCompletionOption.ResponseHeadersRead).AsTask();
            await Task.WhenAll(downloadTasks);

            using (HttpResponseMessage reponse = downloadTasks[0].Result)
            {
                reponse.EnsureSuccessStatusCode();

                var stream = await reponse.Content.ReadAsBufferAsync();
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

                emote.ImageSize1 = bytes;
            }
            using (HttpResponseMessage reponse = downloadTasks[1].Result)
            {
                reponse.EnsureSuccessStatusCode();

                var stream = await reponse.Content.ReadAsBufferAsync();
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

                emote.ImageSize2 = bytes;
            }
            using (HttpResponseMessage reponse = downloadTasks[2].Result)
            {
                reponse.EnsureSuccessStatusCode();

                var stream = await reponse.Content.ReadAsBufferAsync();
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

                emote.ImageSize4 = bytes;
            }
#else
            using HttpResponseMessage reponse = await client.GetAsync(new(jsonEmote.images.url_1x), HttpCompletionOption.ResponseHeadersRead);

            reponse.EnsureSuccessStatusCode();

            var stream = await reponse.Content.ReadAsBufferAsync();
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

            emote.Image = bytes;
#endif
        }
    }
}
