using Multitool.NTInterop;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation.Metadata;

namespace Multitool.DAL
{
    /// <summary>
    /// Calculates the size of a directory.
    /// </summary>
    public class DirectorySizeCalculator : IProgressNotifier
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DirectorySizeCalculator() { }

        /// <inheritdoc/>
        public bool Notify { get; set; }

        /// <inheritdoc/>
        public event TaskProgressEventHandler Progress;
        
        /// <inheritdoc/>
        public event TaskFailedEventHandler Exception;

        /// <summary>
        /// Calculate the size of a directory asynchronously, updating size in real time through <paramref name="setter"/>.
        /// </summary>
        /// <param name="path">Directory to calculate the size of</param>
        /// <param name="setter"></param>
        /// <param name="cancellationToken">Cancellation token</param>
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

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string[] files = Directory.GetFiles(path);
                    long fileSize = 0;
                    for (int i = 0; i < files.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InvokeProgressAsync(files[i]);

                        try
                        {
                            fileSize = new FileInfo(files[i]).Length;
                        }
                        catch (OperationFailedException e)
                        {
                            InvokeExceptionAsync(e);
                        }
                    }
                    setter(fileSize);
                }
                catch (UnauthorizedAccessException e)
                {
                    InvokeExceptionAsync(e);
                }
                catch (FileNotFoundException e)
                {
                    InvokeExceptionAsync(e);
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
            await Task.WhenAll(tasks);
            tasks.Clear();
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

        /// <summary>
        /// Calculate the size of a directory. The method will not return the size of the directory
        /// it will instead call <paramref name="setter"/> with the value to add to the current size.
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <param name="cancellationToken">Cancellation token to cancel the method</param>
        /// <param name="setter"></param>
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
                    CalculateDirectorySize(subDirs[i], setter, cancellationToken);
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
                    catch (OperationFailedException e)
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

        /// <summary>
        /// Calculate the size of a directory.
        /// </summary>
        /// <param name="path">Path to the directory</param>
        /// <param name="cancellationToken">Cancellation token to cancel the method</param>
        /// <returns>The size of the directory <paramref name="path"/></returns>
        [Deprecated("Use async methods", DeprecationType.Deprecate, 0)]
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
