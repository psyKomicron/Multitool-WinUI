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
    public sealed class TwitchEmoteProxy : IDisposable
    {
        private const int timeout = 500;
        private static TwitchEmoteProxy instance;

        private readonly System.Timers.Timer cacheTimer;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores = new();

        private readonly SemaphoreSlim globalEmotesDownloadSemaphore = new(1);
        private readonly SemaphoreSlim emoteCacheSemaphore = new(1);

        private readonly List<Emote> globalEmotesCache = new();
        private readonly ConcurrentBag<Emote> emotesCache = new();

        private EmoteFetcher emoteFetcher;

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

        public EmoteFetcher EmoteFetcher => emoteFetcher;

        public static TwitchEmoteProxy GetInstance()
        {
            if (instance is null)
            {
                instance = new();
            }
            return instance;
        }

        public void CreateEmoteFetcher(TwitchConnectionToken token)
        {
            if (emoteFetcher == null)
            {
                emoteFetcher = new(token);
            }
        }

        public void Dispose()
        {
            emoteFetcher.Dispose();
        }

        public async Task<List<Emote>> GetGlobalEmotes()
        {
            CheckToken();

            if (globalEmotesCache.Count > 0)
            {
                Trace.TraceInformation("Cache built, returning emote cache");
                return globalEmotesCache;
            }
            else if (globalEmotesDownloadSemaphore.CurrentCount == 1)
            {
                try
                {
                    Trace.TraceInformation("Proxy downloading emotes...");

                    await globalEmotesDownloadSemaphore.WaitAsync();

                    globalEmotesCache.AddRange(await emoteFetcher.GetGlobalTwitchEmotes());
                    globalEmotesDownloadSemaphore.Release();

                    Trace.TraceInformation($"Downloaded global emotes (count {globalEmotesCache.Count})");

                    return globalEmotesCache;
                }
                catch
                {
                    globalEmotesDownloadSemaphore.Release();
                    throw;
                }
            }
            else
            {
                try
                {
                    Trace.TraceInformation("Waiting for cache to be build...");

                    // is there a better way to do that ?
                    await globalEmotesDownloadSemaphore.WaitAsync();
                    globalEmotesDownloadSemaphore.Release();

                    Trace.TraceInformation("Cache built, returning");

                    return globalEmotesCache;
                }
                catch
                {
                    globalEmotesDownloadSemaphore.Release();
                    throw;
                }
            }
        }

        public async Task<List<Emote>> GetChannelEmotes(string channel)
        {
            // not the way i want to do it. Maybe will copy behavior from Multitool.DAL.FileSystem
            CheckToken();
            if (semaphores.TryGetValue(channel, out SemaphoreSlim semaphore))
            {
                Trace.TraceInformation("Semaphore exists, checking emotes status");

                if (await semaphore.WaitAsync(50_000))
                {
                    semaphore.Release();
                    try
                    {
                        if (await emoteCacheSemaphore.WaitAsync(timeout))
                        {
                            Trace.TraceInformation($"Getting emotes for {channel} from cache...");
                            List<Emote> channelEmotes = new(10);
                            for (int i = 0; i < emotesCache.Count; i++)
                            {
                                if (emotesCache.TryPeek(out Emote emote) && emote.ChannelOwner == channel)
                                {
                                    channelEmotes.Add(emote);
                                }
                            }
                            emoteCacheSemaphore.Release();

                            return channelEmotes;
                        }
                        else
                        {
                            throw new OperationCanceledException("Operation cancelled after timeout.");
                        }
                    }
                    catch
                    {
                        semaphore.Release();
                        throw;
                    }
                }
                else
                {
                    throw new OperationCanceledException("Operation cancelled after 500ms timeout.");
                }
            }
            else
            {
                // download emotes
                Trace.TraceInformation("Semaphore doesn't exist, downloading emotes");

                SemaphoreSlim s = new(1);
                semaphores.TryAdd(channel, s);
                await s.WaitAsync();
                List<Emote> channelEmotes = await emoteFetcher.GetTwitchChannelEmotes(channel);

                Trace.TraceInformation("Downloaded emotes");

                if (await emoteCacheSemaphore.WaitAsync(timeout))
                {
                    Trace.TraceInformation($"Caching emotes from #{channel}...");
                    for (int i = 0; i < channelEmotes.Count; i++)
                    {
                        emotesCache.Add(channelEmotes[i]);
                    }
                    emoteCacheSemaphore.Release();
                    Trace.TraceInformation($"Cached emotes from #{channel}");
                }
                else
                {
                    Trace.TraceWarning($"Failed to get emote cache semaphore, not caching channel emotes (count: {channelEmotes.Count}, channel: {channel})");
                }
                s.Release();
                return channelEmotes;
            }
        }

        public async Task<List<Emote>> GetEmoteSets(string sets)
        {
            throw new NotImplementedException();
        }

        #region private methods
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

        #region event handlers
        private void OnCacheTimerElapsed(object sender, ElapsedEventArgs e)
        {

        }
        #endregion
    }
}
