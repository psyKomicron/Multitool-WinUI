using System.IO;

namespace Multitool.FileSystem
{
    public class WatcherDelegates
    {
        public FileSystemEventHandler DeletedHandler { get; set; }
        public FileSystemEventHandler CreatedHandler { get; set; }
        public FileSystemEventHandler ChangedHandler { get; set; }
        public RenamedEventHandler RenamedHandler { get; set; }
    }
}
