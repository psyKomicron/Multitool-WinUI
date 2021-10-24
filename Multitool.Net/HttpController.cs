using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Multitool.Net
{
    /// <summary>
    /// Wrapper around .NET's <see cref="HttpClient"/> to avoid socket over-allocation
    /// </summary>
    public sealed class HttpController : IDisposable
    {
        private volatile bool _disposed;
        private static readonly HttpClient _client = new();

        public HttpController() { }

        #region properties

        public static IWebProxy DefaultProxy { get; set; }

        public HttpVersionPolicy DefaultVersionPolicy { get; set; }

        public Version DefaultRequestVersion { get; set; }

        public HttpRequestHeaders DefaultRequestHeaders { get; set; }

        public Uri? BaseAddress { get; set; }

        public long MaxResponseContentBufferSize { get; set; }

        public TimeSpan Timeout { get; set; }
        #endregion

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