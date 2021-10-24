using System;

namespace Multitool.DAL.FileSystem.Events
{
    /// <summary>
    /// Provides data for cache updating event.
    /// </summary>
    public class CacheUpdatingEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="path">The path updating</param>
        public CacheUpdatingEventArgs(string path) : base()
        {
            Path = path;
        }

        /// <summary>
        /// Path updating.
        /// </summary>
        public string Path { get; }
    }
}