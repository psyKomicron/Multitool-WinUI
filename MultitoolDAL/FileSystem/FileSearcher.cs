using Multitool.Data.Win32;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.Data.FileSystem
{
    public class FileSearcher : IDisposable
    {
        private static readonly Regex audioRegex = new(@"^audio/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex videoRegex = new(@"^video/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex textRegex = new(@"^text/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex appRegex = new(@"^application/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ConcurrentQueue<string> pathes = new();
        private int threadCount = 10;
        private bool threadsStarted;
        private string rootPath;

        public FileSearcher() { }

        public FileSearcher(Regex ignoreList) : this()
        {
            IgnoreList = ignoreList;
        }

        #region properties
        public Regex IgnoreList { get; set; }

        public int ThreadCount
        {
            get => threadCount;
            set
            {
                if (threadsStarted)
                {
                    throw new InvalidOperationException("Thread count cannot be altered when they are already started.");
                }
                threadCount = value;
            }
        }

        public string Root
        {
            get => rootPath;
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    throw new ArgumentException($"{value} does not exists, please provide an existing path.");
                }
                rootPath = value;
            }
        } 
        #endregion

#if true
        public event TypedEventHandler<FileSearcher, Tuple<int, string>> ThreadProgress;
#endif

        public async Task<List<string>> SearchForType(FileType type)
        {
            string[] extensions = GetExtensions(type);
            bool filter(FileInfo file)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    if (file.Extension == extensions[i])
                    {
                        return true;
                    }
                }
                return false;
            }
            return await Search(filter);
        }

        public async Task<List<string>> SearchForType(FileType type, Predicate<FileInfo> predicate)
        {
            string[] extensions = GetExtensions(type);
            bool filter(FileInfo file)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    if (file.Extension == extensions[i])
                    {
                        return predicate(file);
                    }
                }
                return false;
            }
            return await Search(filter);
        }

        public async Task<List<string>> Search(Predicate<FileInfo> filter)
        {
            threadsStarted = true;
            using SemaphoreSlim semaphore = new(0);
            List<Thread> threads = CreateThreads();
            QueuePathes();
            Trace.TraceInformation($"Enqueued {pathes.Count} pathes for search");
            Trace.TraceInformation($"Starting threads, count {ThreadCount}.");
            List<string>[] files = new List<string>[threadCount];
            for (int i = 0; i < threads.Count; i++)
            {
                Thread t = threads[i];
                List<string> localFiles = new();
                files[i] = localFiles;
                t.Start(new ThreadStartParameter(semaphore, localFiles, filter));
            }

            int completed = 0;
            while (completed < threadCount)
            {
                await semaphore.WaitAsync();
                completed++;
            }

            Trace.TraceInformation("Threads finished, merging entries...");
            List<string> total = new(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                total.AddRange(files[i]);
            }
            threadsStarted = false;
            return total;
        }

        public void Dispose()
        {
        }

        private List<Thread> CreateThreads()
        {
            List<Thread> threads = new(threadCount);
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new(ThreadStart);
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.AboveNormal;
                threads.Add(thread);
#if DEBUG
                ThreadProgress?.Invoke(this, new(thread.ManagedThreadId, "Created")); 
#endif
            }
            return threads;
        }

        private void QueuePathes()
        {
            if (Root == null)
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                for (int i = 0; i < drives.Length; i++)
                {
                    DirectoryInfo[] rootDirs = drives[i].RootDirectory.GetDirectories();
                    for (int j = 0; j < rootDirs.Length; j++)
                    {
                        if (IgnoreList == null || !IgnoreList.IsMatch(rootDirs[j].FullName))
                        {
                            try
                            {
                                DirectoryInfo[] dirs = rootDirs[j].GetDirectories();
                                for (int k = 0; k < dirs.Length; k++)
                                {
                                    if (IgnoreList == null || !IgnoreList.IsMatch(dirs[k].FullName))
                                    {
                                        pathes.Enqueue(dirs[k].FullName); 
                                    }
                                    else
                                    {
                                        Trace.TraceInformation($"Ignoring {dirs[k].FullName}.");
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                Trace.TraceError($"{rootDirs[j]} access denied.");
                            } 
                        }
                        else
                        {
                            Trace.TraceInformation($"Ignoring {rootDirs[j].FullName}.");
                        }
                    }
                }
            }
            else
            {
                DirectoryInfo root = new(Root);
                DirectoryInfo[] rootDirs = root.GetDirectories();
                for (int j = 0; j < rootDirs.Length; j++)
                {
                    try
                    {
                        DirectoryInfo[] dirs = rootDirs[j].GetDirectories();
                        for (int k = 0; k < dirs.Length; k++)
                        {
                            pathes.Enqueue(dirs[k].FullName);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Trace.TraceError($"{rootDirs[j]} access denied.");
                    }
                }
            }
        }

        private List<string> GetFiles(DirectoryInfo root, Predicate<FileInfo> predicate, int progressId)
        {
            ThreadProgress?.Invoke(this, new(progressId, root.FullName));
            List<string> validFiles = new();
            try
            {
                DirectoryInfo[] dirs = root.GetDirectories();
                for (int i = 0; i < dirs.Length; i++)
                {
                    var results = GetFiles(dirs[i], predicate, progressId);
                    if (results.Count > 0)
                    {
                        validFiles.AddRange(results);
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }

            FileInfo[] files = root.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    if (IgnoreList != null)
                    {
                        if (!IgnoreList.IsMatch(files[i].Name) && predicate(files[i]))
                        {
                            validFiles.Add(files[i].FullName);
                        }
                    }
                    else if (predicate(files[i]))
                    {
                        validFiles.Add(files[i].FullName);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (FileNotFoundException) { }
            }
            return validFiles;
        }

        private void ThreadStart(object o)
        {
            Trace.TraceInformation($"{Thread.CurrentThread.ManagedThreadId} started.");
            ThreadProgress?.Invoke(this, new(Thread.CurrentThread.ManagedThreadId, $"{Thread.CurrentThread.ManagedThreadId} started."));

            ThreadStartParameter startParameter = (ThreadStartParameter)o;
            List<string> files = startParameter.Files;
            Stopwatch watch = Stopwatch.StartNew();
            while (!pathes.IsEmpty)
            {
                if (pathes.TryDequeue(out string path))
                {
                    ThreadProgress?.Invoke(this, new(Thread.CurrentThread.ManagedThreadId, path));
                    try
                    {
                        files.AddRange(GetFiles(new DirectoryInfo(path), startParameter.Filter, Thread.CurrentThread.ManagedThreadId));
                    }
                    catch (DirectoryNotFoundException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }

            watch.Stop();
            Trace.TraceInformation($"{Thread.CurrentThread.ManagedThreadId} finished, computed {files.Count} files in {watch.Elapsed}.");
            ThreadProgress?.Invoke(this, new(Thread.CurrentThread.ManagedThreadId, $"{Thread.CurrentThread.ManagedThreadId} finished, computed {files.Count} files in {watch.Elapsed}."));

            startParameter.Semaphore.Release();
        }

        private static string[] GetExtensions(FileType type)
        {
            Regex regex = null;
            switch (type)
            {
                case FileType.Video:
                    regex = videoRegex;
                    break;
                case FileType.Audio:
                    regex = audioRegex;
                    break;
                case FileType.Text:
                    regex = textRegex;
                    break;
                case FileType.Executable:
                    regex = appRegex;
                    break;
            }
            return RegistryHelper.GetExtensionsForMime(regex);
        }
    }

    internal struct ThreadStartParameter
    {
        public SemaphoreSlim Semaphore;
        public List<string> Files;
        public Predicate<FileInfo> Filter;

        public ThreadStartParameter(SemaphoreSlim semaphore, List<string> files, Predicate<FileInfo> filter)
        {
            Files = files;
            Semaphore = semaphore;
            Filter = filter;
        }
    }
}
