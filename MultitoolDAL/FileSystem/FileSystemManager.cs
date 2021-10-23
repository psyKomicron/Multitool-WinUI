using Multitool.DAL.FileSystem.Events;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Multitool.DAL.FileSystem
{
    /// <summary>
    /// Class to manage <see cref="IFileSystemEntry"/> with cache and async methods.
    /// </summary>
    public class FileSystemManager : IFileSystemManager
    {
        [Deprecated("Use property or build configuration", DeprecationType.Deprecate, 0)]
#if DEBUG
        public const double DEFAULT_CACHE_TIMEOUT = double.NaN;
#else
        public const double DEFAULT_CACHE_TIMEOUT = 300_000;
#endif
        [Deprecated("Use property or build configuration", DeprecationType.Deprecate, 0)]
        public const bool DEFAULT_NOTIFY_STATUS = false;

        private static readonly Dictionary<string, FileSystemCache> cache = new();
        private readonly object _eventlock = new();
        private readonly DirectorySizeCalculator calculator = new();
        private double _ttl;
        private bool _notify;

        #region ctors
        /// <summary>
        /// Default constructor with default cache TTL and with <see cref="Notify"/> set to <see langword="false"/>.
        /// </summary>
        public FileSystemManager()
        {
            _ttl = DEFAULT_CACHE_TIMEOUT;
            Notify = DEFAULT_NOTIFY_STATUS;
            calculator.Notify = DEFAULT_NOTIFY_STATUS;
            calculator.Progress += OnCalculatorProgress;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ttl"></param>
        /// <param name="notifyProgress"></param>
        public FileSystemManager(double ttl, bool notifyProgress)
        {
            _ttl = ttl;
            Notify = notifyProgress;
            calculator.Notify = notifyProgress;
            calculator.Progress += OnCalculatorProgress;
        }
        #endregion

        #region properties
        public bool Notify
        {
            get => _notify;
            set
            {
                _notify = value;
                calculator.Notify = value;
            }
        }

        public double CacheTimeout
        {
            get => _ttl;
            set
            {
                _ttl = value;
                foreach (KeyValuePair<string, FileSystemCache> c in cache)
                {
                    c.Value.UpdateTTL(value);
                }
            }
        }
        #endregion

        #region events
        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemManager, ChangeEventArgs> Changed;

        /// <inheritdoc/>
        public event TypedEventHandler<IFileSystemManager, CacheUpdatingEventArgs> CacheUpdating;

        /// <inheritdoc/>
        public event TaskCompletedEventHandler Completed;

        /// <inheritdoc/>
        public event TaskFailedEventHandler Exception
        {
            add
            {
                lock (_eventlock)
                {
                    SelfException += value;
                    calculator.Exception += value;
                }
            }
            remove
            {
                lock (_eventlock)
                {
                    calculator.Exception -= value;
                    SelfException -= value;
                }
            }
        }

        /// <inheritdoc/>
        public event TaskProgressEventHandler Progress;

        private event TaskFailedEventHandler SelfException;
        #endregion

        #region public
        /// <inheritdoc/>
        public string GetRealPath(string path)
        {
            string realPath;
            if (Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new(path);
                List<string> parents = new(5);

                DirectoryInfo parent = directoryInfo.Parent;

                if (parent != null)
                {
                    IEnumerable<DirectoryInfo> directories = parent.EnumerateDirectories();
                    foreach (DirectoryInfo fileInfo in directories)
                    {
                        if (fileInfo.Name.Equals(directoryInfo.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            parents.Add(fileInfo.Name);
                            break;
                        }
                    }

                    while (parent != null && parent.Parent != null)
                    {
                        if (parent.Parent != null)
                        {
                            directories = parent.Parent.EnumerateDirectories();
                            foreach (DirectoryInfo fileInfo in directories)
                            {
                                if (fileInfo.Name.Equals(parent.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    parents.Add(fileInfo.Name);
                                    break;
                                }
                            }
                        }

                        parent = parent.Parent;
                    }
                    if (parent != null)
                    {
                        parents.Add(parent.Name.ToUpperInvariant());
                    }

                    StringBuilder stringBuilder = new();
                    for (int i = parents.Count - 1; i >= 0; i--)
                    {
                        _ = stringBuilder.Append(parents[i]);
                        if (!parents[i].Contains(Path.DirectorySeparatorChar.ToString()) && i != 0)
                        {
                            _ = stringBuilder.Append(Path.DirectorySeparatorChar);
                        }
                    }

                    realPath = stringBuilder.ToString();
                }
                else
                {
                    return path;
                }
            }
            else
            {
                throw new DirectoryNotFoundException("Directory not found, path : " + path);
            }
            return realPath;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            Trace.TraceInformation("Resetting filesystem manager");
            foreach (KeyValuePair<string, FileSystemCache> pair in cache)
            {
                FileSystemCache cache = pair.Value;
                cache.Delete();
            }
            cache.Clear();
            Trace.TraceInformation("Reset successful");
        }

        /// <inheritdoc/>
        public async Task GetFileSystemEntries<TItem>(string path, IList<TItem> list, AddDelegate<TItem> addDelegate, CancellationToken cancellationToken) where TItem : IFileSystemEntry
        {
            #region not null
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list), "List cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A path needs to be provided (path was either null, empty, or with spaces only). Provided path: " + path, nameof(path));
            }
#endregion

            if (cache.ContainsKeyInvariant(path))
            {
                FileSystemCache fileCache = cache.Get(path);

                if (fileCache.Frozen)
                {
                    Trace.TraceInformation("Cache frozen, recreating one and getting files");
                    cache.Remove(fileCache.Path);
                    fileCache.Delete();
                    fileCache.Dispose();
                    await GetAll(path, list, addDelegate, new FileSystemCache(path, CacheTimeout), cancellationToken);
                }
                else
                {
#if false
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < fileCache.Count; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            addDelegate(list, fileCache[i]);
                        }
                    }, cancellationToken);
#endif
                    if (fileCache.Partial)
                    {
                        await GetPartial(path, fileCache, list, addDelegate, cancellationToken);
                    }
#if true
                    else
                    {
                        await Task.Run(() =>
                        {
                            for (int i = 0; i < fileCache.Count; i++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                addDelegate(list, fileCache[i]);
                            }
                        }, cancellationToken);
                    }
#endif
                    InvokeCompletion(TaskStatus.RanToCompletion);
                }
            }
            else if (Directory.Exists(path))
            {
                try
                {
                    await GetAll(path, list, addDelegate, new(path, CacheTimeout), cancellationToken);
                }
                catch (InvalidOperationException e)
                {
                    Trace.TraceError("Unable to create cache. Exception :\n" + e.ToString());
                }
            }
        }

        #region DEBUG
#if DEBUG
        public static void RefreshAllCache()
        {
            foreach (KeyValuePair<string, FileSystemCache> item in cache)
            {
                RefreshCache(item.Value);
            }
        }

        public static void RefreshCache(string path)
        {
            RefreshCache(cache[path]);
        }

        internal static void RefreshCache(FileSystemCache fsCache)
        {
            for (int i = 0; i < fsCache.Count; i++)
            {
                FileSystemEntry entry = fsCache[i];
                entry.Partial = true;
                entry.RefreshInfos();
            }
            if (fsCache.Frozen)
            {
                fsCache.UnFreeze();
            }
        }
#endif
        #endregion

        #endregion

        #region private

        #region file get
        internal async Task GetAll<ItemType>(string path, IList<ItemType> list, AddDelegate<ItemType> addDelegate, FileSystemCache fileCache, CancellationToken cancellationToken) where ItemType : IFileSystemEntry
        {
            cache.Add(path, fileCache);
            fileCache.Changed += OnCacheItemChanged;
            fileCache.Updating += OnCacheUpdating;
            fileCache.Deleted += OnCacheDeleted;

            GetFiles(path, fileCache, list, addDelegate, cancellationToken);
            await GetDirectories(Directory.GetDirectories(path), fileCache, list, addDelegate, cancellationToken);
            fileCache.Partial = false;
            InvokeCompletion(TaskStatus.RanToCompletion);
        }

        private async Task GetPartial<T>(string path, FileSystemCache cacheItems, IList<T> list, AddDelegate<T> addDelegate, CancellationToken cancellationToken) where T : IFileSystemEntry
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<string> paths = new(Directory.GetFileSystemEntries(path));
            List<string> toDo = new(paths.Count - cacheItems.Count + 1);
            // get partial items
            for (int i = 0; i < cacheItems.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IFileSystemEntry cacheItem = cacheItems[i];
                if (cacheItem.Partial)
                {
                    toDo.Add(cacheItem.Path);
                    cacheItems.RemoveAt(i);
                    paths.Remove(cacheItem.Path);
                }
            }
            // get the missing file entries
            for (int i = 0; i < paths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string filePath = paths[i];
                bool contains = false;
                //List<Task> addTasks = new();
                for (int j = 0; j < cacheItems.Count; j++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (cacheItems[j].Path.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    {
                        contains = true;
                        int currIndex = j;
#if false
                        Task t = new(() => addDelegate(list, cacheItems[currIndex]), cancellationToken);
                        addTasks.Add(t);
                        t.Start();
#endif
                        _ = Task.Run(() => addDelegate(list, cacheItems[currIndex]), cancellationToken);
                        break;
                    }
                }
                if (!contains)
                {
                    toDo.Add(filePath);
                }
            }

            try
            {
                for (int i = 0; i < toDo.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InvokeProgress(toDo[i]);

                    if (File.Exists(toDo[i]))
                    {
                        FileEntry item = new(new(toDo[i]));
                        cacheItems.Add(item);
#if DEBUG
                        _ = Task.Run(() => addDelegate(list, item), cancellationToken);
#endif
                    }
                    else if (Directory.Exists(toDo[i]))
                    {
                        DirectoryEntry item = new(new(toDo[i]));
                        cacheItems.Add(item);
#if DEBUG
                        _ = Task.Run(() => addDelegate(list, item), cancellationToken);
#endif
                        await calculator.CalculateDirectorySizeAsync(toDo[i], (long newSize) => item.Size += newSize, cancellationToken);
                        item.Partial = false;
                    }
                }
                toDo.Clear();
                cacheItems.Partial = false;
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeException(e);
            }
        }

        private void GetFiles<T>(string path, FileSystemCache cacheItems, IList<T> list, AddDelegate<T> addDelegate, CancellationToken cancellationToken) where T : IFileSystemEntry
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i < files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgress(files[i]);

                    FileSystemEntry item = new FileEntry(new FileInfo(files[i]));
                    cacheItems.Add(item);
                    addDelegate(list, item);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeException(e);
            }
        }

        private async Task GetDirectories<T>(string[] dirPaths, FileSystemCache cacheItems, IList<T> list, AddDelegate<T> addDelegate, CancellationToken cancellationToken) where T : IFileSystemEntry
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                List<Task> tasks = new(dirPaths.Length);
                for (int i = 0; i < dirPaths.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgress(dirPaths[i]);

                    string currentPath = dirPaths[i];
                    FileSystemEntry item = new DirectoryEntry(new(currentPath));
                    tasks.Add(CalculateDirSizeParallel(item, currentPath, cancellationToken));

                    cacheItems.Add(item);
                    addDelegate(list, item);
                }
                await Task.WhenAll(tasks);
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeException(e);
            }
        }

        private async Task CalculateDirSizeParallel(FileSystemEntry item, string currentPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await calculator.CalculateDirectorySizeAsync(currentPath, (long newSize) => item.Size += newSize, cancellationToken);
                item.Partial = false;
            }
            catch (AggregateException e)
            {
                e.Data.Add(GetType(), "Uncommon aggregate exception (from calculating dir size). Path :" + currentPath);
                Trace.TraceError(e.ToString());
                InvokeException(e);
            }
        }
        #endregion

        #region events invoke
        private void InvokeException(Exception e)
        {
            if (Notify)
            {
                _ = Task.Run(() => SelfException?.Invoke(this, e));
            }
        }

        private void InvokeProgress(string message)
        {
            if (Notify)
            {
                _ = Task.Run(() => Progress?.Invoke(this, message));
            }
        }

        private void InvokeCompletion(TaskStatus status)
        {
            if (Notify)
            {
                Completed?.Invoke(status);
            }
        }
        #endregion

        #endregion

        #region events handlers
        private void OnCacheUpdating(FileSystemCache sender, EventArgs e)
        {
            CacheUpdating?.Invoke(this, new(sender.Path));
        }

        private void OnCacheItemChanged(FileSystemCache sender, CacheChangedEventArgs args)
        {
            /*if (args.ChangeType != ChangeTypes.FileRenamed)
            {
                Task.Run(() =>
                {
                    Changed?.Invoke(this, new(args.Entry, args.ChangeType));
                    args.InUse = false;
                });
            }*/
            args.InUse = false;
        }

        private void OnCacheDeleted(FileSystemCache sender, EventArgs args)
        {
            if (cache.ContainsKey(sender.Path))
            {
                cache.Remove(sender.Path);
                Trace.TraceInformation("Removed " + sender.Path + " (cache deleted)");
            }
        }

        private void OnCalculatorProgress(object sender, string message)
        {
            InvokeProgress(message);
        }
        #endregion
    }
}
