using Multitool.DAL.Events;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using Windows.Foundation;
using Multitool.DAL.FileSystem.Events;
using Multitool.Optimisation;

namespace Multitool.DAL.FileSystem
{
    /// <summary>
    /// Provides logic to watch a directory for changes, and to signal the changes.
    /// <see cref="FileSystemCache"/> will only signal the changes and not update itself.
    /// </summary>
    internal class FileSystemCache : IDisposable
    {
        private static readonly object _lock = new();
        private static readonly List<string> watchedPaths = new();
        private readonly DirectorySizeCalculator calculator = new();
        private readonly List<FileSystemEntry> watchedItems;
        private readonly FileSystemWatcher watcher;
        private readonly ObjectPool<CacheChangedEventArgs> cacheChangedPool = new(7);
        private System.Timers.Timer timer;
        private double ttl;

        /// <summary>Constuctor.</summary>
        /// <param name="path">File path to monitor</param>
        /// <param name="ttl">Cache time-to-live</param>
        public FileSystemCache(string path, double ttl)
        {
            CheckAndAddPath(path);

            Partial = true;
            this.ttl = ttl;
            Path = path;
            watchedItems = new List<FileSystemEntry>(10);

            try
            {
                watcher = WatcherFactory.CreateWatcher(path, new WatcherDelegates()
                {
                    ChangedHandler = OnFileChange,
                    CreatedHandler = OnFileCreated,
                    DeletedHandler = OnFileDeleted,
                    RenamedHandler = OnFileRenamed
                });
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception) // catching Exception because we are re-throwing it
            {
                lock (_lock)
                {
                    _ = watchedPaths.Remove(path);
                }
                throw;
            }

            if (!double.IsNaN(ttl))
            {
                CreateTimer();
                timer.Start();
            }
            CreationTime = DateTime.UtcNow;
        }

        #region properties
        /// <summary>Gets the internal item count.</summary>
        public int Count => watchedItems.Count;

        /// <summary>Tells if the cache allow operations on it or not (true if no operation are allowed).</summary>
        public bool Frozen { get; private set; }

        /// <summary>True when the cache is not complete.</summary>
        public bool Partial { get; set; }

        /// <summary>Gets the creation time of the cache.</summary>
        public DateTime CreationTime { get; }

        public string Path { get; }

        public FileSystemEntry this[int index] => watchedItems[index];
        #endregion

        #region events
        /// <summary>
        /// Raised when the watched items underwent a change, and should be updated.
        /// </summary>
        public event TypedEventHandler<FileSystemCache, CacheChangedEventArgs> Changed;

        /// <summary>
        /// Raised whenever the cache TTL reached 0, and thus should be updated.
        /// </summary>
        public event TypedEventHandler<FileSystemCache, TTLReachedEventArgs> TTLReached;

        /// <summary>
        /// Raised when the directory (<see cref="Path"/>) is deleted.
        /// </summary>
        public event TypedEventHandler<FileSystemCache, EventArgs> Deleted;
        #endregion

        #region public
        /// <inheritdoc/>
        public void Dispose()
        {
            watcher.Dispose();
        }

        /// <summary>Unfroze the <see cref="FileSystemCache"/> to re-allow operations on it.</summary>
        public void UnFreeze()
        {
            Trace.TraceInformation("Unfreezing cache for " + Path);
            if (timer != null)
            {
                timer.Interval = ttl;
            }
            Frozen = false;
        }

        /// <summary>Add an <see cref="FileSystemEntry"/> to the internal collection.</summary>
        /// <param name="item"><see cref="FileSystemEntry"/> to add</param>
        public void Add(FileSystemEntry item)
        {
            CheckIfFrozen();
            if (timer != null)
            {
                ResetTimer();
            }
            lock (_lock)
            {
                if (timer != null && !timer.Enabled)
                {
                    timer.Start();
                }

                if (watchedItems.Contains(item))
                {
                    throw new ArgumentException("Item already in the collection");
                }
                else
                {
                    watchedItems.Add(item);
                }
            }
            Changed?.Invoke(this, cacheChangedPool.GetObject(item, ChangeTypes.FileCreated));
        }

        /// <summary>Remove a <see cref="FileSystemEntry"/> from the collection.</summary>
        /// <param name="item"><see cref="FileSystemEntry"/> to remove</param>
        /// <returns>True if the item was removed, False if not</returns>
        public bool Remove(FileSystemEntry item)
        {
            CheckIfFrozen();
            lock (_lock)
            {
                return watchedItems.Remove(item);
            }
        }

        public void RemoveAt(int i)
        {
            CheckIfFrozen();
            lock (_lock)
            {
                watchedItems.RemoveAt(i);
            }
        }

        /// <summary>Changes the time to live (TTL) value for the cache. Changing the value will act as if the TTL was reached.</summary>
        /// <param name="newTTL">The new TTL</param>
        public void UpdateTTL(double newTTL)
        {
            CheckIfFrozen();
            ttl = newTTL;
            TTLReached?.Invoke(this, new(Path, ttl, true));
        }

        /// <summary>Use to discard the cache.</summary>
        public void Delete()
        {
            Frozen = true;
            watcher.EnableRaisingEvents = false;
            lock (_lock)
            {
                watchedItems.Clear();
                _ = watchedPaths.Remove(Path);
            }
            if (timer != null)
            {
                timer.Stop();
            }
            Deleted?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region private methods

        private static void CheckAndAddPath(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException(path + " does not exist. Please provide a valid path.", new DirectoryNotFoundException());
            }
            if (File.Exists(path))
            {
                throw new ArgumentException(path + " is a file, please provide a directory to watch.", new FileNotFoundException());
            }

            lock (_lock)
            {
                foreach (string watchedPath in watchedPaths)
                {
                    if (string.Equals(watchedPath, path, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(path + " is already being monitored.");
                    }
                }
                watchedPaths.Add(path);
            }
        }

        private void ResetTimer()
        {
            timer.Stop();
            timer.Interval = ttl;
        }

        private void CheckIfFrozen()
        {
            if (Frozen)
            {
                throw new InvalidOperationException("Cache is frozen.");
            }
        }

        private void CreateTimer()
        {
            timer = new System.Timers.Timer(ttl);
            timer.Elapsed += OnTimerElapsed;
            timer.AutoReset = true;
        }

        private string DumpWatcher()
        {
            string dump = "Path: " + watcher.Path + "\nFilters: ";
            NotifyFilters filters = watcher.NotifyFilter;
            if ((filters & NotifyFilters.FileName) != 0)
            {
                dump += "FileName,";
            }
            if ((filters & NotifyFilters.DirectoryName) != 0)
            {
                dump += "DirectoryName,";
            }
            if ((filters & NotifyFilters.Attributes) != 0)
            {
                dump += "Attributes,";
            }
            if ((filters & NotifyFilters.Size) != 0)
            {
                dump += "Size,";
            }
            if ((filters & NotifyFilters.LastWrite) != 0)
            {
                dump += "LastWrite,";
            }
            if ((filters & NotifyFilters.LastAccess) != 0)
            {
                dump += "LastAccess,";
            }
            if ((filters & NotifyFilters.CreationTime) != 0)
            {
                dump += "CreationTime,";
            }
            if ((filters & NotifyFilters.Security) != 0)
            {
                dump += "Security,";
            }
            return dump;
        }

        #endregion

        #region events handlers
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Frozen = true;
            if (timer != null)
            {
                timer.Stop();
            }
            TTLReached?.Invoke(this, new(Path, ttl));

            Trace.TraceInformation("Cache TTL reached, updating " + Path);
            for (int i = 0; i < watchedItems.Count; i++)
            {
                FileSystemEntry item = watchedItems[i];
                Trace.TraceInformation("\tupdating " + item.Name);
                item.RefreshInfos();
            }
            UnFreeze();
        }

        #region watcher events
        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("File changed: '" + e.FullPath + "' (" + (Frozen ? "cache frozen" : "cache hot") + ")");
            if (!Frozen)
            {
                if (timer != null)
                {
                    ResetTimer();
                }

                FileSystemEntry item = watchedItems.Find(v => v.Path == e.FullPath);
                if (timer != null)
                {
                    timer.Start();
                }
                Changed?.Invoke(this, cacheChangedPool.GetObject(item, e.ChangeType));
            }
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("File created: '" + e.FullPath + "'");
            if (!Frozen)
            {
                if (timer != null)
                {
                    ResetTimer();
                }

                FileSystemEntry entry;
                if (Directory.Exists(e.FullPath)) // Check if the added item is a directory
                {
                    entry = new DirectoryEntry(new(e.FullPath))
                    {
                        Partial = true
                    };
                    entry.Size = await calculator.CalculateDirectorySizeAsync(e.FullPath, CancellationToken.None);
                    entry.Partial = false;
                }
                else
                {
                    entry = new FileEntry(new FileInfo(e.FullPath));
                }
                Add(entry);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Trace.TraceInformation("File deleted: '" + e.FullPath + "'");
            if (!Frozen)
            {
                if (timer != null)
                {
                    ResetTimer();
                }

                FileSystemEntry deletedItem = watchedItems.Find(v => v.Path.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
                _ = watchedItems.Remove(deletedItem);
                Changed?.Invoke(this, cacheChangedPool.GetObject(deletedItem, WatcherChangeTypes.Deleted));
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Trace.TraceInformation("File renamed: \"" + e.OldFullPath + "\" to \"" + e.FullPath + "\"");
            if (!Frozen)
            {
                if (timer != null)
                {
                    ResetTimer();
                }
                FileSystemEntry item = watchedItems.Find(v => v.Path.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.Name = e.Name;
                    item.Path = e.FullPath;
                }
                Changed?.Invoke(this, cacheChangedPool.GetObject(item, WatcherChangeTypes.Renamed));
                if (timer != null)
                {
                    timer.Start();
                }
            }
#if DEBUG
            else
            {
                Trace.TraceWarning("File renamed (: " + e.OldFullPath + " -> " + e.FullPath + ") | cache is frozen");
            }
#endif
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            if (e.GetException() != null)
            {
                Exception ex = e.GetException();
                ex.Data.Add("Watcher dump", DumpWatcher());
                throw ex;
            }
            else
            {
                throw new Win32Exception("Watcher error (no exception).\n" + DumpWatcher());
            }
        }
        #endregion

        #endregion
    }
}
