using Multitool.Optimisation;

namespace Multitool.Data.FileSystem.Events
{
    internal class CacheChangedEventArgs : PoolableObject
    {
        public CacheChangedEventArgs() : base()
        {
            ChangeType = ChangeTypes.None;
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
