using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Multitool.Net.Imaging
{
    public class TwitchEmoteProxy
    {
        private const int timeout = 500;
        private static TwitchEmoteProxy instance;

        private readonly System.Timers.Timer cacheTimer;
        private readonly ConcurrentDictionary<object, Emote> cache = new();
        private readonly EmoteFetcher emoteFetcher;

        private readonly SemaphoreSlim globalEmotesDownloadSemaphore = new(0);
        private readonly List<Emote> globalEmotesCache = new();

        private readonly List<Emote> emotesCache = new();

        protected TwitchEmoteProxy()
        {
            cacheTimer = new(5_000);
            cacheTimer.Elapsed += OnCacheTimerElapsed;
        }

        public TwitchConnectionToken ConnectionToken 
        { 
            get => emoteFetcher.ConnectionToken; 
            set
            {
                emoteFetcher.ConnectionToken = value;
            } 
        }

        public EmoteFetcher EmoteFetcher => emoteFetcher;

        public static TwitchEmoteProxy GetInstance()
        {
            if (instance is null)
            {
                instance = new();
            }
            return instance;
        }

        public async Task<List<Emote>> GetGlobalEmotes()
        {
            CheckToken();
            if (globalEmotesCache.Count > 0)
            {
                return globalEmotesCache;
            }
            else if (globalEmotesDownloadSemaphore.CurrentCount == 0)
            {
                globalFetchTask = emoteFetcher.GetGlobalEmotes();
                List<Emote> emotes = await globalFetchTask;
                Trace.TraceInformation($"Downloaded global emotes (count {emotes.Count})");
                return emotes;
            }
            else
            {
                return await globalFetchTask;
            }
        }

        public async Task<List<Emote>> GetChannelEmotes(string channel)
        {
            CheckToken();
            throw new NotImplementedException();
        }

        public async Task<List<Emote>> GetEmoteSets(string sets)
        {

        }

        #region private methods
        private void CheckToken()
        {
            if (ConnectionToken == null)
            {
                throw new ArgumentNullException(nameof(ConnectionToken));
            }
            if (!ConnectionToken.Validated)
            {
                throw new ArgumentException($"Connection token needs to be validated before it can be used: {nameof(ConnectionToken.Validated)} = {ConnectionToken.Validated}");
            }
        }
        #endregion

        #region event handlers
        private void OnCacheTimerElapsed(object sender, ElapsedEventArgs e)
        {

        }
        #endregion
    }
}
