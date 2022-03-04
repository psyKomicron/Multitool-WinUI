
using Multitool.Net.Properties;
using Multitool.Net.Irc.Security;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Windows.Foundation.Metadata;
using Windows.Media.Protection.PlayReady;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    public sealed class EmoteProxy : IEmoteFetcher, IDisposable
    {
        private static EmoteProxy instance;
        private const int timeout = 500; //ms
        private const int fileStreamBufferSize = 0x2000;

        private readonly System.Timers.Timer cacheTimer;
        // concurrent channel emote downloading
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores = new();

        private readonly SemaphoreSlim globalEmotesDownloadSemaphore = new(1);
        private readonly SemaphoreSlim emoteCacheSemaphore = new(1);

        private readonly List<Emote> globalEmotesCache = new();
        private readonly ConcurrentBag<Emote> emotesCache = new();
        private readonly HashAlgorithm hasher = SHA256.Create();
        private readonly string cacheFolderPath;

        private EmoteProxy()
        {
            cacheFolderPath = string.Format(Resources.EmoteCacheFolder, ApplicationData.Current.TemporaryFolder.Path);
            if (!Directory.Exists(cacheFolderPath))
            {
                Trace.TraceInformation("Creating emote cache folder");
                Directory.CreateDirectory(cacheFolderPath);
                Trace.TraceInformation($"Created emote cache folder: \"{cacheFolderPath}\".");
            }

            cacheTimer = new(5_000);
            cacheTimer.Elapsed += OnCacheTimerElapsed;
            EmoteFetchers = new(3);
        }

        public List<EmoteFetcher> EmoteFetchers { get; init; }
        public string Provider { get; }

        public void Dispose()
        {
            cacheTimer.Elapsed -= OnCacheTimerElapsed;
            cacheTimer.Dispose();

            hasher.Dispose();

            emotesCache.Clear();
            globalEmotesCache.Clear();
            semaphores.Clear();

            for (int i = 0; i < EmoteFetchers.Count; i++)
            {
                EmoteFetchers[i].Dispose();
            }
        }

        public static EmoteProxy Get(TwitchConnectionToken token = null, bool ignoreValidation = false)
        {
            if (instance == null)
            {
                if (token == null)
                {
                    throw new ArgumentNullException(nameof(token));
                }
                if (!ignoreValidation && !token.Validated)
                {
                    throw new ArgumentException("Connection token needs to be validated.");
                }

                instance = new();
                
                instance.EmoteFetchers.Add(new FfzEmoteFetcher());
                instance.EmoteFetchers.Add(new SevenTVEmoteFetcher());
                instance.EmoteFetchers.Add(new TwitchEmoteFetcher(token));
                instance.EmoteFetchers.Add(new BttvEmoteFetcher(token));
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

                TimeSpan mean = new();
                Stopwatch watch = new();
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
                            watch.Restart();
                            await CacheEmotes(result);
                            watch.Stop();
                            if (mean.Ticks == 0)
                            {
                                mean = watch.Elapsed;
                            }
                            else
                            {
                                mean.Add(watch.Elapsed);
                            }
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
                            builder.AppendLine(ex.ToString());
                            builder.AppendLine("Exception data:");
                            foreach (var d in ex.Data)
                            {
                                string key = ((KeyValuePair<object, object>)d).Key.ToString();
                                string value = ((KeyValuePair<object, object>)d).Value.ToString();
                                builder.Append('\t').Append(key).Append(' ').Append('=').Append(' ').AppendLine(value);
                            }
                            Trace.TraceError(builder.ToString());
                        }
                        else
                        {
                            Trace.TraceError(ex.ToString());
                        }
                    } 
                    #endregion
                }

                globalEmotesDownloadSemaphore.Release();
                Trace.TraceInformation($"Downloaded and cached {globalEmotesCache.Count} emotes in {mean:T}.");
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

        [Deprecated("Not stable", DeprecationType.Remove, 1)]
        public async Task<List<Emote>> FetchChannelEmotes(string channel)
        {
            if (semaphores.TryGetValue(channel, out SemaphoreSlim semaphore))
            {
                if (await semaphore.WaitAsync(60_000))
                {
                    semaphore.Release();
                    try
                    {
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
                List<string> ids = new();
                List<Task<List<Emote>>> tasks = new();
                for (int i = 0; i < EmoteFetchers.Count; i++)
                {
                    EmoteFetchers[i].
                }

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

        public Task<List<Emote>> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public Task<List<Emote>> GetEmoteSets(string sets)
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

        private async Task CacheEmotes(List<Emote> emotes)
        {
            Trace.TraceInformation($"Caching {emotes.Count} emotes from {emotes[0].Provider}");
            using HttpClient client = new();
            Encoding encoding = Encoding.UTF8;
            List<Task> cacheTask = new(emotes.Count);
            for (int i = 0; i < emotes.Count; i++)
            {
                Emote emote = emotes[i];
                cacheTask.Add(CacheEmote(emote, encoding, client));
            }
            await Task.WhenAll(cacheTask);
        }

        private async Task CacheEmote(Emote emote, Encoding encoding, HttpClient client)
        {
            byte[] arr = await emote.GetHashCode(hasher, encoding);
            string hash = CryptographicBuffer.EncodeToHexString(CryptographicBuffer.CreateFromByteArray(arr));
            string path =  cacheFolderPath + hash;
            if (!File.Exists(path))
            {
                try
                {
                    // Check performance with File.Create(string path, int bufferSize)
                    using FileStream stream = File.Create(path, fileStreamBufferSize, FileOptions.WriteThrough | FileOptions.SequentialScan);
                    using BinaryWriter writer = new(stream);

                    Task<byte[]> imageData = emote.GetImageAsync(client, ImageSize.Medium);
                    writer.Write(await imageData);
                    emote.SetImage(new(path));

                    stream.Close();
                }
                catch (IOException ex)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    Trace.TraceError($"{ex}");
                }
            }
            else
            {
                emote.SetImage(new(path));
            }
        }

        #region event handlers
        private void OnCacheTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // check for new emotes, unused ones...
        }
        #endregion

    }
}
