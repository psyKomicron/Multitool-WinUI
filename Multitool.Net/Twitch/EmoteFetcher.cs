using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Twitch.Json;
using Multitool.Net.Twitch.Security;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Twitch
{
    /// <summary>
    /// Gets emotes from Twitch's API
    /// </summary>
    public class EmoteFetcher : IDisposable
    {
        private readonly HttpClient client = new();
        private bool disposed;
        private readonly TwitchConnectionToken token;

        public EmoteFetcher(TwitchConnectionToken connectionToken)
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

        public async Task<List<Emote>> GetAllEmotes()
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

            string emotes = await emotesResponse.Content.ReadAsStringAsync();
            List<JsonEmote> list = JsonConvert.DeserializeObject<List<JsonEmote>>(emotes);


            for (int i = 0; i < list.Count; i++)
            {
                Emote emote = new();
                emote.Id = new(list[i].id);
                emote.Name = list[i].name;

                using HttpResponseMessage emoteData = await client.GetAsync(new(Properties.Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);
                emotesResponse.EnsureSuccessStatusCode();
                IBuffer buffer = await emoteData.Content.ReadAsBufferAsync();
                
                using DataReader dataReader = DataReader.FromBuffer(buffer);

            }
            return new();
        }

        private void CheckIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
