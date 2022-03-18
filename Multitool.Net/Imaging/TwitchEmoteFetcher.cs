using Multitool.Net.Imaging.Json;
using Multitool.Net.Properties;
using Multitool.Net.Irc.Security;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.IO;
using System.Drawing;

namespace Multitool.Net.Imaging
{
    /// <summary>
    /// Gets emotes from Twitch's API.
    /// </summary>
    public class TwitchEmoteFetcher : EmoteFetcher
    {
        private readonly TwitchApiHelper helper;
        private TwitchConnectionToken connectionToken;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="connectionToken">The API connection token.</param>
        public TwitchEmoteFetcher(TwitchConnectionToken connectionToken) : base(new())
        {
            ConnectionToken = connectionToken;
            Provider = "Twitch";
            Client.DefaultRequestHeaders.Authorization = new("Bearer", ConnectionToken.Token);
            Client.DefaultRequestHeaders.Add(new("Client-Id", ConnectionToken.ClientId));
            helper = new(Client);
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

        /// <inheritdoc/>
        public override async Task<Emote[]> FetchGlobalEmotes()
        {
            CheckIfDisposed();
            CheckToken();

            TwitchJsonData data = await GetJsonAsync<TwitchJsonData>(Resources.TwitchApiGlobalEmotesEndPoint);
            return GetEmotes(data, "Global").ToArray();
        }

        /// <inheritdoc/>
        public override async Task<Emote[]> FetchChannelEmotes(string channel)
        {
            CheckIfDisposed();
            CheckToken();

            string url = string.Format(Resources.TwitchApiChannelEmoteEndPoint, await helper.GetUserId(channel));
            var data = await GetJsonAsync<TwitchJsonData>(url);
            return GetEmotes(data, "Channel").ToArray();
        }

        /// <inheritdoc/>
        public override Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override async Task<List<string>> FetchChannelEmotesIds(string channel)
        {
            string url = string.Format(Resources.TwitchApiChannelEmoteEndPoint, await helper.GetUserId(channel));
            TwitchJsonData data = await GetJsonAsync<TwitchJsonData>(url);
            List<string> strings = new();
            var emotes = data.data;
            for (int i = 0; i < emotes.Count; i++)
            {
                strings.Add(emotes[i].id);
            }
            return strings;
        }

        #region private members
        private List<Emote> GetEmotes(TwitchJsonData data, string emoteType)
        {
            List<TwitchJsonEmote> list = data.data;
            List<Emote> emotes = new();

            for (int i = 0; i < list.Count; i++)
            {
                TwitchJsonEmote jsonEmote = list[i];
                Dictionary<Size, string> urls = new();
                urls.Add(new(28, 28), jsonEmote.images.url_1x);
                urls.Add(new(56, 56), jsonEmote.images.url_2x);
                urls.Add(new(128, 128), jsonEmote.images.url_4x);

                Emote emote = new(new(jsonEmote.id), jsonEmote.name, urls)
                {
                    Provider = Provider,
                    Type = emoteType
                };
                emotes.Add(emote);
            }
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
