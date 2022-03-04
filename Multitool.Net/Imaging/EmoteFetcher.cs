using Multitool.Net.Imaging.Json.Ffz;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    public abstract class EmoteFetcher : IDisposable, IEmoteFetcher
    {
        private readonly HttpClient client = new();
        private bool disposed;

        public EmoteFetcher(HttpClient client)
        {
            this.client = client;
        }

        /// <inheritdoc/>
        public string Provider { get; protected set; }

        protected HttpClient Client => client;

        /// <inheritdoc/>
        public abstract Task<List<Emote>> FetchGlobalEmotes();

        /// <inheritdoc/>
        public abstract Task<List<Emote>> FetchChannelEmotes(string channel);

        /// <inheritdoc/>
        public abstract Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except);

        /// <inheritdoc/>
        public abstract Task<List<string>> ListChannelEmotes(string channel);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
            }
            disposed = true;
            GC.SuppressFinalize(this);
        }

        protected void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected async Task<TJson> GetJsonAsync<TJson>(string fetchLink)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, new(fetchLink));
            var httpResponse = await Client.SendRequestAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
#if false
            string s = await httpResponse.Content.ReadAsStringAsync();
            TJson json = JsonSerializer.Deserialize<TJson>(s);
            return json;
#else
            var inputStream = await httpResponse.Content.ReadAsInputStreamAsync();
            using var stream = inputStream.AsStreamForRead();
            TJson json = await JsonSerializer.DeserializeAsync<TJson>(stream);
            return json;
#endif
        }

#if false
        protected async Task DownloadEmoteAsync(Emote emote, Uri emoteUrl, string mimeType)
        {
            try
            {
                using HttpResponseMessage reponse = await client.GetAsync(emoteUrl, HttpCompletionOption.ResponseHeadersRead).AsTask();
                if (reponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    Trace.TraceError($"Bad request:\n{await reponse.Content.ReadAsStringAsync()}\n{reponse.Headers}");
                }
                reponse.EnsureSuccessStatusCode();

                // Can we use the already buffered data to build the image
                IBuffer stream = await reponse.Content.ReadAsBufferAsync();
                //var mem = Windows.Storage.Streams.Buffer.CreateMemoryBufferOverIBuffer(stream);
                using DataReader dataReader = DataReader.FromBuffer(stream);

                byte[] bytes = new byte[dataReader.UnconsumedBufferLength];
                int pos = 0;
                while (dataReader.UnconsumedBufferLength > 0)
                {
                    bytes[pos] = dataReader.ReadByte();
                    pos++;
                }

                await emote.SetImageAsync(bytes, mimeType);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to download/set emote '{emote.Name}': {ex.Message}");
                throw;
            }
        } 
#endif
    }
}