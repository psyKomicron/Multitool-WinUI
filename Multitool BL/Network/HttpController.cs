using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Multitool.Net
{
    public sealed class HttpController : IDisposable
    {
        private bool _disposed;
        private static readonly HttpClient _client = new();

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Dispose();
                _disposed = true;
            }
        }

        public Task<string> GetStringAsync(Uri uri)
        {
            return _client.GetStringAsync(uri);
        }
    }
}
