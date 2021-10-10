using Multitool.Optimisation;

namespace Multitool.DAL.Events
{
    internal class TTLReachedEventArgs : PoolableObject
    {
        public TTLReachedEventArgs(string path, double ttl, bool ttlUpdated = false) : base()
        {
            Path = path;
            TTLUpdated = ttlUpdated;
            TTL = ttl;
        }

        public string Path { get; private set; }

        public bool TTLUpdated { get; private set; }

        public double TTL { get; private set; }
    }
}
