using Multitool.Net.Twitch;
using Multitool.Net.Twitch.Security;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Multitool.Net.Imaging
{
    public class TwitchEmoteProxy
    {
        private static TwitchEmoteProxy instance;

        private readonly Timer cacheTimer;
        private readonly ConcurrentDictionary<object, Emote> cache = new();
        private readonly List<Emote> globalEmotesCache;
        private readonly EmoteFetcher emoteFetcher;

        private TwitchEmoteProxy()
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

        public static TwitchEmoteProxy GetInstance()
        {
            if (instance is null)
            {
                instance = new();
            }
            return instance;
        }

        public async Task<List<Emote>> LoadGlobalEmotes()
        {
            CheckToken();
            if (globalEmotesCache.Count > 0)
            {
                return globalEmotesCache;
            }
            else
            {
                List<Emote> emotes = await emoteFetcher.GetGlobalEmotes();
                return emotes;
            }
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
