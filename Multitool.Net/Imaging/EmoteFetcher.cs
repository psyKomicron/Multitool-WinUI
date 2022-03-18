using Multitool.Net.Imaging.Json.Ffz;
using Multitool.Net.Irc;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        #region properties
        /// <inheritdoc/>
        public string Provider { get; protected set; }

        protected HttpClient Client => client; 
        #endregion

        #region abstract methods
        /// <inheritdoc/>
        public abstract Task<Emote[]> FetchGlobalEmotes();

        /// <inheritdoc/>
        public abstract Task<Emote[]> FetchChannelEmotes(string channel);

        /// <inheritdoc/>
        public abstract Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except);

        /// <summary>
        /// List the channel's available emotes for the implementation's emote provider.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public abstract Task<List<string>> FetchChannelEmotesIds(string channel); 
        #endregion

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

        protected static void AssertChannelValid(string channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }
            if (string.IsNullOrWhiteSpace(channel) || !Regex.IsMatch(channel, @"^#*[A-z0-9_].{2,24}$"))
            {
                throw new ArgumentException($"Channel name is not valid. Value : {channel}.");
            }
        }

        protected void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

#nullable enable
        /// <summary>
        /// Performs a GET request to <paramref name="fetchLink"/> and deserializes the JSON content received into <typeparamref name="TJson"/>.
        /// <para/>
        /// The method will return <see langword="null"/> if the server responds with 404/NotFound.
        /// </summary>
        /// <typeparam name="TJson">Type to deserialize the JSON to</typeparam>
        /// <param name="fetchLink">Resource to GET</param>
        /// <returns>The deserialized data, or <see langword="null"/> if the server responds with 404/NotFound.</returns>
        protected async Task<TJson?> GetJsonAsync<TJson>(string fetchLink)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, new(fetchLink));
            HttpResponseMessage httpResponse = await Client.SendRequestAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            httpResponse.EnsureSuccessStatusCode();

            var inputStream = await httpResponse.Content.ReadAsInputStreamAsync();
            using var stream = inputStream.AsStreamForRead();
            TJson? json = await JsonSerializer.DeserializeAsync<TJson>(stream);
            return json;
        }
#nullable disable
    }
}