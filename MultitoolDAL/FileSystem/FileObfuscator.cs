using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.Security.Cryptography;

namespace Multitool.Data.FileSystem
{
    public class FileObfuscator : IDisposable
    {
        private readonly Thread[] threads;
        private bool disposed;
        private long stopRequested = 0;

        public FileObfuscator() : this(2)
        {
        }

        public FileObfuscator(int numberOfWorkerThreads)
        {
            if (numberOfWorkerThreads > 10)
            {
                Trace.TraceWarning("A high number of threads can negatively impact performance.");
            }

            threads = new Thread[numberOfWorkerThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new(ThreadWorker);
            }
        }

        public HashAlgorithm HashAlgorithm { get; init; } = SHA256.Create();
        public Encoding Encoding { get; init; } = Encoding.UTF8;

        public void Dispose()
        {
            if (!disposed)
            {
                // to cancel running threads
                Interlocked.Exchange(ref stopRequested, 1);
                disposed = true;
            }
        }

        public async Task<string> Obfuscate(string path)
        {
            string name = Path.GetFileName(path);
            string newName = CreateHash(name);

            int i = path.Length - 1;
            for (; i >= 0; i--)
            {
                if (path[i] == Path.DirectorySeparatorChar || path[i] == Path.AltDirectorySeparatorChar)
                {
                    i++;
                    break;
                }
            }
            string newPath = $"{path[0..i]}{newName}";

            using FileStream fileStream = File.OpenRead(path);
            using FileStream newFileStream = File.Create(newPath, 20_000, FileOptions.Asynchronous);
            fileStream.CopyTo(newFileStream);
            await newFileStream.FlushAsync();

            return newPath;
        }

        public async Task<List<string>> Obfuscate(List<string> pathes)
        {
            throw new NotImplementedException();
        }

        private void ThreadWorker(object o = null)
        {

        }

        private string CreateHash(string toHash)
        {
            byte[] hash = HashAlgorithm.ComputeHash(Encoding.GetBytes(toHash));
            return CryptographicBuffer.EncodeToHexString(CryptographicBuffer.CreateFromByteArray(hash));
        }
    }
}
