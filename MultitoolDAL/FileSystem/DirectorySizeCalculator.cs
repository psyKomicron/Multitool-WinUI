using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Multitool.DAL
{
    /// <summary>
    /// 
    /// </summary>
    public class DirectorySizeCalculator : IProgressNotifier
    {
        /// <summary>
        /// 
        /// </summary>
        public DirectorySizeCalculator() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notify"></param>
        public DirectorySizeCalculator(bool notify)
        {
            Notify = notify;
        }

        /// <inheritdoc/>
        public bool Notify { get; set; }

        /// <inheritdoc/>
        public event TaskProgressEventHandler Progress;
        
        /// <inheritdoc/>
        public event TaskFailedEventHandler Exception;

        /// <summary>
        /// Calculate the size of a directory asynchronously, directly pumping the size in real time.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="setter"></param>
        public async Task CalculateDirectorySizeAsync(string path, Action<long> setter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<Task> tasks = new();

            try
            {
                try
                {
                    string[] subDirs = Directory.GetDirectories(path);
                    for (int i = 0; i < subDirs.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InvokeProgressAsync(subDirs[i]);

                        string subPath = subDirs[i];
                        Task t = new(() => CalculateDirectorySize(subPath, setter, cancellationToken), cancellationToken);
                        tasks.Add(t);
                        t.Start();
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    InvokeExceptionAsync(uae);
                }
                catch (DirectoryNotFoundException de)
                {
                    InvokeExceptionAsync(de);
                }

                await Task.WhenAll(tasks);
                tasks.Clear();

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string[] files = Directory.GetFiles(path);
                    for (int i = 0; i < files.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InvokeProgressAsync(files[i]);

                        try
                        {
                            string filePath = files[i];
                            Task t = new(() => setter(new FileInfo(filePath).Length), cancellationToken);
                            tasks.Add(t);
                            t.Start();
                        }
                        catch (FileNotFoundException e)
                        {
                            InvokeExceptionAsync(e);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            InvokeExceptionAsync(e);
                        }
                    }
                }
                catch (UnauthorizedAccessException e)
                {
                    InvokeExceptionAsync(e);
                }
                catch (FileNotFoundException e)
                {
                    InvokeExceptionAsync(e);
                }

                await Task.WhenAll(tasks);
                tasks.Clear();
            }
            catch (OperationCanceledException opCanceled)
            {
                opCanceled.Data.Add(GetType(), "Operation cancelled while waiting for children threads (calculating " + path + ")");
                throw;
            }
            catch (AggregateException aggregate)
            {
                for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                {
                    if (aggregate.InnerExceptions[i].GetType() == typeof(OperationCanceledException))
                    {
                        aggregate.InnerExceptions[i].Data.Add(GetType(), "Operation cancelled while waiting for children threads (calculating " + path + ")");
                        throw aggregate.InnerExceptions[i];
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<long> CalculateDirectorySizeAsync(string path, CancellationToken cancellationToken)
        {
            long size = 0;
            cancellationToken.ThrowIfCancellationRequested();
            List<Task<long>> tasks = new();

            try
            {
                string[] subDirs = Directory.GetDirectories(path);
                for (int i = 0; i < subDirs.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(subDirs[i]);

                    string subPath = subDirs[i];
                    Task<long> t = new(() => CalculateDirectorySize(subPath, cancellationToken), cancellationToken);
                    tasks.Add(t);
                    t.Start();
                }

                try
                {
                    long[] results = await Task.WhenAll(tasks);
                    for (int i = 0; i < results.Length; i++)
                    {
                        size += results[i];
                    }
                }
                catch (OperationCanceledException opCanceled)
                {
                    opCanceled.Data.Add(GetType(), "Operation cancelled while waiting for children threads (calculating " + path + ")");
                    throw;
                }
                catch (AggregateException aggregate)
                {
                    for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                    {
                        if (aggregate.InnerExceptions[i].GetType() == typeof(OperationCanceledException))
                        {
                            aggregate.InnerExceptions[i].Data.Add(GetType(), "Operation cancelled while waiting for children threads (calculating " + path + ")");
                            throw aggregate.InnerExceptions[i];
                        }
                    }

                    throw;
                }
                finally
                {
                    tasks.Clear();
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (DirectoryNotFoundException de)
            {
                InvokeExceptionAsync(de);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] files = Directory.GetFiles(path);

                for (int i = 0; i < files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(files[i]);
                    try
                    {
                        size += new FileInfo(files[i]).Length;
                    }
                    catch (FileNotFoundException e)
                    {
                        InvokeExceptionAsync(e);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (FileNotFoundException fe)
            {
                InvokeExceptionAsync(fe);
            }

            return size;
        }

        public void CalculateDirectorySize(string path, Action<long> setter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string[] subDirs = Directory.GetDirectories(path);
                for (int i = 0; i < subDirs.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(subDirs[i]);
                    setter(CalculateDirectorySize(subDirs[i], cancellationToken));
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (DirectoryNotFoundException de)
            {
                InvokeExceptionAsync(de);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i < files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(files[i]);
                    try
                    {
                        setter(new FileInfo(files[i]).Length);
                    }
                    catch (FileNotFoundException e)
                    {
                        InvokeExceptionAsync(e);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (FileNotFoundException fe)
            {
                InvokeExceptionAsync(fe);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        public long CalculateDirectorySize(string path, CancellationToken cancellationToken)
        {
            long size = 0;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string[] subDirs = Directory.GetDirectories(path);
                for (int i = 0; i < subDirs.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(subDirs[i]);
                    size += CalculateDirectorySize(subDirs[i], cancellationToken);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (DirectoryNotFoundException de)
            {
                InvokeExceptionAsync(de);
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i < files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    InvokeProgressAsync(files[i]);
                    try
                    {
                        size += new FileInfo(files[i]).Length;
                    }
                    catch (FileNotFoundException e)
                    {
                        InvokeExceptionAsync(e);
                    }
                }
            }
            catch (UnauthorizedAccessException e)
            {
                InvokeExceptionAsync(e);
            }
            catch (FileNotFoundException fe)
            {
                InvokeExceptionAsync(fe);
            }
            cancellationToken.ThrowIfCancellationRequested();

            return size;
        }

        private void InvokeExceptionAsync(Exception e)
        {
            if (Notify)
            {
                _ = Task.Run(() => Exception?.Invoke(this, e));
            }
        }

        private void InvokeProgressAsync(string message)
        {
            if (Notify)
            {
                _ = Task.Run(() => Progress?.Invoke(this, message));
            }
        }
    }
}
