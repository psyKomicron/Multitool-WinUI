using System;
using System.IO;

namespace Multitool.DAL.Events
{
    internal delegate void CacheChangedEventHandler(
        object sender, string name, FileSystemEntry entry, bool ttl, WatcherChangeTypes changes);
}
