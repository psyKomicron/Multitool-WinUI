using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public ImageSize DefaultImageSize { get; set; } = ImageSize.Medium;

        protected HttpClient Client => client;

        public abstract Task<List<Emote>> FetchGlobalEmotes();

        public abstract Task<List<Emote>> FetchChannelEmotes(string channel);

        public abstract Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except);

        public abstract Task<List<string>> ListChannelEmotes(string channel);

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
                    if (pos >= bytes.Length)
                    {
                        Trace.TraceWarning("Index out of bounds");
                        break;
                    }
                    bytes[pos] = dataReader.ReadByte();
                    pos++;
                }

                await emote.SetImage(bytes, mimeType);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to download and set emote '{emote.Name}'. Exception:\n{ex}");
                throw;
            }
        }

    }
}