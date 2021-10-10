using Multitool.Optimisation;

namespace Multitool.DAL.FileSystem.Events
{
    internal class CacheChangedEventArgs : PoolableObject
    {
        public CacheChangedEventArgs() : base()
        {
            ChangeType = ChangeTypes.All;
        }

        public CacheChangedEventArgs(FileSystemEntry entry, ChangeTypes changeType) : base()
        {
            Entry = entry;
            ChangeType = changeType;
        }

        public FileSystemEntry Entry { get; set; }

        public ChangeTypes ChangeType { get; set; }
    }
}
