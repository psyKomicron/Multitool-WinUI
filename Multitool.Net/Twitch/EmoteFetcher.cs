using Microsoft.UI.Xaml.Controls;

using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public void Dispose()
        {
            client.Dispose();
            disposed = true;
        }

        public async Task<List<Emote>> GetAllEmotes()
        {
            CheckIfDisposed();

            client.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
            client.DefaultRequestHeaders.Add(new("Client-Id", token.ClientId));

            using HttpResponseMessage response = await client.GetAsync(new(Properties.Resources.TwitchApiGlobalEmotesEndPoint), HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();
            client.DefaultRequestHeaders.Clear();

            var emotes = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Emotes payload length {emotes.Length}");

            return null;
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
