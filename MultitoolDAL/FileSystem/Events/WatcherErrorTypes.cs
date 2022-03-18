namespace Multitool.Data.FileSystem.Events
{
    /// <summary>
    /// Lists types of <see cref="System.IO.FileSystemWatcher"/> errors.
    /// </summary>
    public enum WatcherErrorTypes : uint
    {
        /// <summary>
        /// Buffer error.
        /// </summary>
        BufferError = 0x0,
        /// <summary>
        /// The path that the watcher was watching has been deleted (analog to <see cref="ChangeTypes.PathDeleted"/>).
        /// </summary>
        PathDeleted = 0x1
    }
}
