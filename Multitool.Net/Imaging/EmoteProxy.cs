
using System;
using System.Collections;
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
    public sealed class EmoteProxy : IEmoteFetcher
    {
        private const int timeout = 500;
        private static EmoteProxy instance;

        private readonly System.Timers.Timer cacheTimer;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores = new();

        private readonly SemaphoreSlim globalEmotesDownloadSemaphore = new(1);
        private readonly SemaphoreSlim emoteCacheSemaphore = new(1);

        private readonly List<Emote> globalEmotesCache = new();
        private readonly ConcurrentBag<Emote> emotesCache = new();

        private EmoteProxy()
        {
            cacheTimer = new(5_000);
            cacheTimer.Elapsed += OnCacheTimerElapsed;
            EmoteFetchers = new(3);
        }

        public List<EmoteFetcher> EmoteFetchers { get; init; }
        public ImageSize DefaultImageSize { get; set; }

        public static EmoteProxy Get()
        {
            if (instance is null)
            {
                instance = new();
            }
            return instance;
        }

        public async Task<List<Emote>> FetchGlobalEmotes()
        {
            if (globalEmotesCache.Count > 0)
            {
                return globalEmotesCache;
            }
            else if (globalEmotesDownloadSemaphore.CurrentCount == 1)
            {
                await globalEmotesDownloadSemaphore.WaitAsync();
                Trace.TraceInformation("Downloading global emotes...");

                List<Task<List<Emote>>> tasks = new(EmoteFetchers.Count);
                foreach (var fetcher in EmoteFetchers)
                {
                    tasks.Add(fetcher.FetchGlobalEmotes());
                }

                while (tasks.Count > 0)
                {
                    Task<List<Emote>> completed = await Task.WhenAny(tasks);
                    tasks.Remove(completed);
                    try
                    {
                        var result = completed.Result;
                        if (completed.IsCompletedSuccessfully && result.Count > 0)
                        {
                            globalEmotesCache.AddRange(result);
                        }
                    }
                    #region Exceptions
                    catch (AggregateException ex)
                    {
                        foreach (Exception exInner in ex.InnerExceptions)
                        {
                            if (exInner.Data != null && exInner.Data.Count > 0)
                            {
                                StringBuilder builder = new();
                                if (exInner.Message.EndsWith('\n'))
                                {
                                    builder.Append(exInner.Message);
                                }
                                else
                                {
                                    builder.AppendLine(exInner.Message);
                                }
                                builder.AppendLine("Exception data:");
                                foreach (var d in exInner.Data)
                                {
                                    if (d is DictionaryEntry entry)
                                    {
                                        builder.Append('\t').Append(entry.Key).Append(':').Append(' ');
                                        string value = entry.Value.ToString();
                                        if (value.EndsWith('\n'))
                                        {
                                            builder.Append(value);
                                        }
                                        else
                                        {
                                            builder.AppendLine(value);
                                        }
                                    }
                                }
                                builder.Append(exInner.StackTrace);
                                Trace.TraceError(builder.ToString());
                            }
                            else
                            {
                                Trace.TraceError(ex.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Data != null && ex.Data.Count > 0)
                        {
                            StringBuilder builder = new();
                            builder.Append(ex.ToString());
                            builder.AppendLine("Exception data:");
                            foreach (var d in ex.Data)
                            {
                                builder.Append('t').AppendLine(d.ToString());
                            }
                        }
                        else
                        {
                            Trace.TraceError(ex.ToString());
                        }
                    } 
                    #endregion
                }
                Trace.TraceInformation($"Downloaded {globalEmotesCache.Count} emotes.");
                globalEmotesDownloadSemaphore.Release();
                return globalEmotesCache;
            }
            else
            {
                try
                {
                    // is there a better way to do that ?
                    await globalEmotesDownloadSemaphore.WaitAsync();
                    globalEmotesDownloadSemaphore.Release();
                    return globalEmotesCache;
                }
                catch
                {
                    globalEmotesDownloadSemaphore.Release();
                    throw;
                }
            }
        }

        public async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
            // not the way i want to do it. Maybe will copy behavior from Multitool.DAL.FileSystem
            if (semaphores.TryGetValue(channel, out SemaphoreSlim semaphore))
            {
                if (await semaphore.WaitAsync(60_000))
                {
                    semaphore.Release();
                    try
                    {
#if false
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
#else
                        Trace.TraceInformation($"Getting emotes from #{channel} from cache...");
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
#endif
                    }
                    catch
                    {
                        semaphore.Release();
                        throw;
                    }
                }
                else
                {
                    throw new OperationCanceledException("Operation cancelled after 60s timeout.");
                }
            }
            else
            {
                Trace.TraceInformation("Checking cache for emotes");
                List<string> ids = await ListChannelEmotes(channel);
                int count = ids.Count;
                foreach (var emotes in emotesCache)
                {
                    ids.Remove(emotes.Id.Id);
                    count--;
                }

                // download emotes
                Trace.TraceInformation($"Downloading emotes for #{channel}... (skipping {ids.Count - count} emotes already in cache)");

                SemaphoreSlim s = new(1);
                semaphores.TryAdd(channel, s);
                await s.WaitAsync();
                try
                {
                    List<Task<List<Emote>>> tasks = new(EmoteFetchers.Count);
                    foreach (var fetcher in EmoteFetchers)
                    {
                        try
                        {
                            if (ids.Count == count)
                            {
                                tasks.Add(fetcher.FetchChannelEmotes(channel));
                            }
                            else
                            {
                                tasks.Add(fetcher.FetchChannelEmotes(channel, ids));
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError(ex.ToString());
                        }
                    }
                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }

                    List<Emote> channelEmotes = new();
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        if (tasks[i].IsCompletedSuccessfully)
                        {
                            channelEmotes.AddRange(tasks[i].Result);
                        }
                    }

                    if (await emoteCacheSemaphore.WaitAsync(timeout))
                    {
                        try
                        {
                            Trace.TraceInformation($"Caching emotes from #{channel}...");
                            for (int i = 0; i < channelEmotes.Count; i++)
                            {
                                emotesCache.Add(channelEmotes[i]);
                            }
                        }
                        finally
                        {
                            emoteCacheSemaphore.Release();
                        }
                    }
                    else
                    {
                        Trace.TraceWarning($"Failed to get emote cache semaphore, not caching channel emotes (count: {channelEmotes.Count}, channel: #{channel})");
                    }

                    s.Release();
                    return channelEmotes;
                }
                catch
                {
                    s.Release();
                    throw;
                }
            }
        }

        public async Task<List<Emote>> GetEmoteSets(string sets)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> ListChannelEmotes(string channel)
        {
            List<string> list = new(10 * EmoteFetchers.Count);
            foreach (var fetcher in EmoteFetchers)
            {
                list.AddRange(await fetcher.ListChannelEmotes(channel));
            }
            return list;
        }

        #region event handlers
        private void OnCacheTimerElapsed(object sender, ElapsedEventArgs e)
        {

        }
        #endregion
    }
}
