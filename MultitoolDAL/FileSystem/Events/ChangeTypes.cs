using System;

namespace Multitool.Data.FileSystem.Events
{
    /// <summary>
    /// Change types.
    /// </summary>
    [Flags]
    public enum ChangeTypes
    {
        /// <summary>
        /// When a directory in watched path has been created.
        /// </summary>
        DirectoryCreated,
        /// <summary>
        /// When a file in watched path has been created.
        /// </summary>
        FileCreated,
        /// <summary>
        /// When a file in a watched path has been deleted.
        /// </summary>
        FileDeleted,
        /// <summary>
        /// When a watched path has been deleted.
        /// </summary>
        PathDeleted,
        /// <summary>
        /// When nothing has been done.
        /// </summary>
        None
    }
}
