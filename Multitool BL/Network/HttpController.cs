using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Multitool.Net
{
    /// <summary>
    /// Wrapper around .NET's <see cref="HttpClient"/> to avoid socket over-allocation
    /// </summary>
    public sealed class HttpController : IDisposable
    {
        private bool _disposed;
        private static readonly HttpClient _client = new();

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Wrapper for the <see cref="HttpClient.GetStringAsync(Uri?)"/>
        /// </summary>
        /// <param name="uri">Uri to fetch</param>
        /// <returns><see cref="Task"/> holding the string result</returns>
        public Task<string> GetStringAsync(Uri uri)
        {
            return _client.GetStringAsync(uri);
        }
    }
}
