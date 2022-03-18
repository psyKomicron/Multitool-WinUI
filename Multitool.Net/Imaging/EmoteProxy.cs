
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
using Multitool.Net.Irc;

namespace Multitool.Net.Imaging
{
    public sealed class EmoteProxy : IEmoteFetcher, IDisposable
    {
        private static EmoteProxy instance;
        private const int timeout = 500; //ms
        private const int fileStreamBufferSize = 0x2000;
        private readonly HashAlgorithm hasher = SHA256.Create();
        private readonly System.Timers.Timer cacheTimer;
        private readonly string cacheFolderPath;

        // concurrent channel emote downloading
        private readonly ConcurrentDictionary<string, SemaphoreSlim> channelDownloadsemaphores = new();

        private readonly SemaphoreSlim globalEmotesDownloadSemaphore = new(1);
        //private readonly SemaphoreSlim emoteWriteCacheSemaphore = new(1);

        private readonly List<Emote> globalEmotesCache = new();
        private readonly ConcurrentDictionary<string, Emote> emotesCache = new();


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

        public void Dispose()
        {
            cacheTimer.Elapsed -= OnCacheTimerElapsed;
            cacheTimer.Dispose();

            hasher.Dispose();

            emotesCache.Clear();
            globalEmotesCache.Clear();
            channelDownloadsemaphores.Clear();

            for (int i = 0; i < EmoteFetchers.Count; i++)
            {
                EmoteFetchers[i].Dispose();
            }
        }

        public async Task<Emote[]> FetchGlobalEmotes()
        {
            // is there a better way to do that ?
            if (globalEmotesCache.Count > 0)
            {
                return globalEmotesCache.ToArray();
            }
            else if (globalEmotesDownloadSemaphore.CurrentCount == 1)
            {
                await globalEmotesDownloadSemaphore.WaitAsync();
                Trace.TraceInformation("Downloading global emotes...");

                List<Task<Emote[]>> tasks = new(EmoteFetchers.Count);
                foreach (var fetcher in EmoteFetchers)
                {
                    tasks.Add(fetcher.FetchGlobalEmotes());
                }

                TimeSpan mean = new();
                Stopwatch watch = new();
                while (tasks.Count > 0)
                {
                    Task<Emote[]> completed = await Task.WhenAny(tasks);
                    tasks.Remove(completed);
                    try
                    {
                        var result = completed.Result;
                        if (completed.IsCompletedSuccessfully && result.Length > 0)
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
                return globalEmotesCache.ToArray();
            }
            else
            {
                try
                {
                    await globalEmotesDownloadSemaphore.WaitAsync();
                    globalEmotesDownloadSemaphore.Release();
                    return globalEmotesCache.ToArray();
                }
                catch
                {
                    globalEmotesDownloadSemaphore.Release();
                    throw;
                }
            }
        }

        public async Task<Emote[]> FetchChannelEmotes(string channel)
        {
#if false
            throw new NotImplementedException();
#endif
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel));
            }
            
            if (channelDownloadsemaphores.TryGetValue(channel, out SemaphoreSlim semaphore))
            {
                if (semaphore != null)
                {
                    if (await semaphore.WaitAsync(60_000)) // wait for the previous call to download emotes.
                    {
                        semaphore.Release();
                    }
                    else
                    {
                        throw new OperationCanceledException("Operation cancelled after 60s timeout.");
                    }
                }

                List<Emote> channelEmotes = new();
                List<Task<List<string>>> tasks = new();
                foreach (EmoteFetcher fetcher in EmoteFetchers)
                {
                    tasks.Add(fetcher.FetchChannelEmotesIds(channel));
                }

                var result = await Task.WhenAll(tasks);
                for (int i = 0; i < result.Length; i++)
                {
                    for (int j = 0; j < result[i].Count; j++)
                    {
                        string id = result[i][j];
                        if (id != null)
                        {
                            if (emotesCache.TryGetValue(id, out Emote emote))
                            {
                                channelEmotes.Add(emote);
                            } 
                        }
                        else
                        {
                            Trace.TraceWarning("Id is null.");
                        }
                    }
                }

                return channelEmotes.ToArray();
            }
            else
            {
                SemaphoreSlim s = new(0);
                try
                {
                    if (channelDownloadsemaphores.TryAdd(channel, s))
                    {
                        Trace.TraceInformation($"Sucessfully created semaphore for #{channel}.");
                    }
                    else
                    {
                        Trace.TraceWarning($"Semaphore for #{channel} already exists.");
                    }

                    List<Task<Emote[]>> tasks = new();
                    List<Emote> channelEmotes = new();
                    for (int i = 0; i < EmoteFetchers.Count; i++)
                    {
                        try
                        {
                            Trace.TraceInformation($"Downloading {EmoteFetchers[i].Provider} emotes.");

                            // emote fetching is fast since we only query the servers for the emote data without the image.
                            tasks.Add(EmoteFetchers[i].FetchChannelEmotes(channel));
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"Failed to fetch {EmoteFetchers[i].Provider} channel emotes: {ex}");
                        }
                    }

                    while (tasks.Count > 0)
                    {
                        Task<Emote[]> completed = await Task.WhenAny(tasks);
                        tasks.Remove(completed);
                        if (completed.IsCompletedSuccessfully)
                        {
                            var result = completed.Result;
                            if (result.Length > 0)
                            {
                                channelEmotes.AddRange(result);
                                for (int i = 0; i < result.Length; i++)
                                {
                                    emotesCache.TryAdd(result[i].Id, result[i]);
                                }
                                await CacheEmotes(result); 
                            }
                        }
                        else
                        {
                            Trace.TraceError($"Emote fetching task failed with exception: {completed.Exception}");
                        }
                    }

                    return channelEmotes.ToArray();
                }
                finally
                {
                    s.Release();
                }
            }
        }

        public Task<Emote[]> FetchChannelEmotes(string channel, IReadOnlyList<string> except)
        {
            throw new NotImplementedException();
        }

        public Task<Emote[]> GetEmoteSets(string sets)
        {
            throw new NotImplementedException();
        }

        #region private methods
        private async Task CacheEmotes(Emote[] emotes)
        {
            Trace.TraceInformation($"Caching {emotes.Length} {emotes[0].Provider} emotes.");

            using HttpClient client = new();
            Encoding encoding = Encoding.UTF8;
            List<Task> cacheTask = new(emotes.Length);
            for (int i = 0; i < emotes.Length; i++)
            {
                Emote emote = emotes[i];
                cacheTask.Add(CacheEmote(emote, encoding, client));
            }
            await Task.WhenAll(cacheTask);

            Trace.TraceInformation($"Finished caching {emotes[0].Provider} emotes to disk.");
        }

        private async Task CacheEmote(Emote emote, Encoding encoding, HttpClient client)
        {
            try
            {
                emote.SetSize(ImageSize.Big);
                byte[] arr = await emote.GetHashCode(hasher, encoding);
                string hash = CryptographicBuffer.EncodeToHexString(CryptographicBuffer.CreateFromByteArray(arr));
                string path = cacheFolderPath + hash;
                if (!File.Exists(path))
                {
                    try
                    {
                        Trace.TraceInformation($"{emote.Provider}.{emote.Name} does not exists, caching emote.");
                        // Check performance with File.Create(string path, int bufferSize)
                        using FileStream stream = File.Create(path, fileStreamBufferSize, FileOptions.WriteThrough | FileOptions.SequentialScan);
                        using BinaryWriter writer = new(stream);

                        Task<byte[]> imageData = emote.GetImageAsync(client, ImageSize.Medium);
                        writer.Write(await imageData);
                        emote.SetImage(new(path));

                        stream.Close();
                    }
                    catch (IOException)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        throw;
                    }
                }
                else
                {
                    emote.SetImage(new(path));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error caching {emote.Provider}.{emote.Name} : {ex.Message}");
                throw;
            }
        } 
        #endregion

        #region event handlers
        private void OnCacheTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // check for new emotes, unused ones...
        }
        #endregion

    }
}
