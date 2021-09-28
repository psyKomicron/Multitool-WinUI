using System;

namespace Multitool.DAL.Events
{
    internal delegate void WatcherErrorEventHandler(FileSystemCache sender, Exception e, WatcherErrorTypes errType);
}
