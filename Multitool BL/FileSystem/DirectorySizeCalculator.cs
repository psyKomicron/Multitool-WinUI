using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Multitool.FileSystem
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
            List<Task> dirTasks = new();

            try
            {
                try
                {
                    string[] subDirs = Directory.GetDirectories(path);
                    for (int i = 0; i < subDirs.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InvokeProgressAsync(subDirs[i]);

                        try
                        {
                            string subPath = subDirs[i];
                            dirTasks.Add(Task.Run(() => CalculateDirectorySize(subPath, setter, cancellationToken), cancellationToken));
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            InvokeExceptionAsync(e);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            InvokeExceptionAsync(e);
                        }
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

                await Task.WhenAll(dirTasks);
                Trace.WriteLine("Finished computing directory sizes '" + path + "'");
                dirTasks.Clear();

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
                            dirTasks.Add(Task.Run(() => setter(new FileInfo(filePath).Length), cancellationToken));
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
                catch (FileNotFoundException fe)
                {
                    InvokeExceptionAsync(fe);
                }

                await Task.WhenAll(dirTasks);
                Trace.WriteLine("Finished computing file sizes '" + path + "'");
                dirTasks.Clear();
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
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    List<Task<long>> dirTasks = new();
                    string[] subDirs = Directory.GetDirectories(path);
                    for (int i = 0; i < subDirs.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InvokeProgressAsync(subDirs[i]);
                        string subPath = subDirs[i];
                        Task<long> t = new(() => CalculateDirectorySize(subPath, cancellationToken), cancellationToken);
                        dirTasks.Add(t);
                        t.Start();
                    }

                    Task<long[]> awaitable = Task.WhenAll(dirTasks);
                    dirTasks.Clear();
                    try
                    {
                        awaitable.Wait(cancellationToken);

                        for (int i = 0; i < awaitable.Result.Length; i++)
                        {
                            size += awaitable.Result[i];
                        }
                    }
                    catch (OperationCanceledException opCanceled)
                    {
                        opCanceled.Data.Add(GetType(), "Operation cancelled while waiting for children threads (calculating " + path +")");
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
            }, cancellationToken);
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
                Task.Run(() => Exception?.Invoke(this, e))
                    .ContinueWith((Task previous) =>
                    {
                        if (previous.IsFaulted)
                        {
                            Trace.WriteLine(nameof(DirectorySizeCalculator) + " -> Failed to raise Exception event. Exception " + previous.Exception.ToString());
                        }
                    });
            }
        }

        private void InvokeProgressAsync(string message)
        {
            if (Notify)
            {
                Task.Run(() => Progress?.Invoke(this, message))
                    .ContinueWith((Task previous) =>
                    {
                        if (previous.IsFaulted)
                        {
                            Trace.WriteLine(nameof(DirectorySizeCalculator) + " -> Failed to raise Exception event. Exception " + previous.Exception.ToString());
                        }
                    });
            }
        }
    }
}
