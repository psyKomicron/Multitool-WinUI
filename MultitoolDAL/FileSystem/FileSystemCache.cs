using Multitool.Data.FileSystem.Events;
using Multitool.Optimisation;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Windows.Foundation;

namespace Multitool.Data.FileSystem
{
    /// <summary>
    /// Provides logic to cache and watch a directory for changes, and to signal the changes.
    /// </summary>
    internal class FileSystemCache : IDisposable
    {
        private static readonly object _lock = new();
        private static readonly List<string> watchedPaths = new();

        //private readonly DirectorySizeCalculator calculator = new();
        private readonly List<FileSystemEntry> watchedItems;
        private readonly FileSystemWatcher watcher;
        private readonly ObjectPool<CacheChangedEventArgs> cacheChangedPool = new(7);
        private System.Timers.Timer timer;
        private double ttl;

        private long _frozen;

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
        public FileSystemEntry this[int index] => watchedItems[index];

        /// <summary>Gets the internal item count.</summary>
        public int Count => watchedItems.Count;

        /// <summary>Tells if the cache allow operations on it or not (true if no operation are allowed).</summary>
        public bool Frozen
        {
            get
            {
                return Interlocked.Read(ref _frozen) == 1;
            }
        }

        /// <summary>True when the cache is not complete.</summary>
        public bool Partial { get; set; }

        /// <summary>Gets the creation time of the cache.</summary>
        public DateTime CreationTime { get; }

        public string Path { get; }
        #endregion

        #region events
        /// <summary>
        /// Raised when a change occured. :)
        /// </summary>
        public event TypedEventHandler<FileSystemCache, CacheChangedEventArgs> Changed;

        /// <summary>
        /// Raised whenever the cache TTL reached 0 and the cache is updating.
        /// </summary>
        public event TypedEventHandler<FileSystemCache, EventArgs> Updating;

        /// <summary>
        /// Raised when the cache's directory (<see cref="Path"/>) is deleted.
        /// </summary>
        public event TypedEventHandler<FileSystemCache, EventArgs> Deleted;
        #endregion

        #region public
        /// <inheritdoc/>
        public void Dispose()
        {
            watcher.Dispose();
        }

        /// <summary>
        /// Unfreeze the <see cref="FileSystemCache"/> to re-allow operations on it.
        /// </summary>
        public void UnFreeze()
        {
            Trace.TraceInformation("Unfreezing cache for " + Path);
            if (timer != null)
            {
                timer.Interval = ttl;
            }
            Interlocked.Exchange(ref _frozen, 0);
        }

        /// <summary>
        /// Add an <see cref="FileSystemEntry"/> to the internal collection.
        /// </summary>
        /// <param name="item"><see cref="FileSystemEntry"/> to add</param>
        public void Add(FileSystemEntry item)
        {
            CheckIfFrozen();
            ResetTimer();
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

        /// <summary>
        /// Remove a <see cref="FileSystemEntry"/> from the collection.
        /// </summary>
        /// <param name="item"><see cref="FileSystemEntry"/> to remove</param>
        /// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if not</returns>
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

        /// <summary>
        /// Changes the time to live (TTL) value for the cache. Changing the value will act as if the TTL was reached.
        /// </summary>
        /// <param name="newTTL">The new TTL</param>
        public void UpdateTTL(double newTTL)
        {
            CheckIfFrozen();
            ttl = newTTL;
        }

        /// <summary>
        /// Use to discard the cache.
        /// </summary>
        public void Delete()
        {
            Interlocked.Exchange(ref _frozen, 1);
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
            if (timer != null)
            {
                timer.Stop();
                timer.Interval = ttl;
            }
        }

        private void CheckIfFrozen()
        {
            if (Interlocked.Read(ref _frozen) == 1)
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
            Interlocked.Exchange(ref _frozen, 0);
            timer.Stop();
#if DEBUG
            string path = string.Empty;
            if (Path.Length > 23)
            {
                path = Path[0..10] + "..." + Path[^11..];
            }
            Trace.TraceInformation("Cache [" + path + "] TTL reached, elapsed " + e.SignalTime.Minute + ":" + e.SignalTime.Second + ":" + e.SignalTime.Millisecond);
#else
            Trace.TraceInformation("Cache TTL reached, updating " + Path);
#endif

            Task.Run(() => Updating?.Invoke(this, EventArgs.Empty));

            for (int i = 0; i < watchedItems.Count; i++)
            {
                FileSystemEntry item = watchedItems[i];
                item.RefreshInfos();
            }

            UnFreeze();
        }

        #region watcher events
        private void OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (!Frozen)
            {
                ResetTimer();
                FileSystemEntry item = watchedItems.Find(v => v.Path == e.FullPath);
                if (item != null)
                {
                    item.RefreshInfos();
                }

                if (timer != null)
                {
                    timer.Start();
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!Frozen)
            {
                ResetTimer();

                if (Directory.Exists(e.FullPath)) // Check if the added item is a directory
                {
                    DirectoryEntry entry = new(new(e.FullPath))
                    {
                        Partial = true
                    };
                    Changed?.Invoke(this, new(entry, ChangeTypes.DirectoryCreated));
                }
                else
                {
                    FileEntry entry = new(new FileInfo(e.FullPath));
                    Add(entry);
                }
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!Frozen)
            {
                ResetTimer();

                FileSystemEntry deletedItem = watchedItems.Find(v => v.Path.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));
                _ = watchedItems.Remove(deletedItem);
                Changed?.Invoke(this, cacheChangedPool.GetObject(deletedItem, WatcherChangeTypes.Deleted));
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!Frozen)
            {
                ResetTimer();
                FileSystemEntry item = watchedItems.Find(v => v.Path.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.Name = e.Name;
                    item.Path = e.FullPath;
                }
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
                if (e.GetException().InnerException == null && !File.Exists(Path))
                {
                    Deleted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Exception ex = e.GetException();
                    ex.Data.Add("Watcher dump", DumpWatcher());
                    throw ex;
                }
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
