﻿namespace Multitool.DAL.FileSystem.Events
{
    /// <summary>
    /// Provides data for file system changes events.
    /// </summary>
    public class ChangeEventArgs
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ChangeEventArgs()
        {
            Entry = null;
            ChangeTypes = ChangeTypes.All;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="changeTypes"></param>
        public ChangeEventArgs(FileSystemEntry entry, ChangeTypes changeTypes) : base()
        {
            Entry = entry;
            ChangeTypes = changeTypes;
        }

        /// <summary>
        /// Entry associated with the event
        /// </summary>
        public IFileSystemEntry Entry { get; }

        /// <summary>
        /// Why this event was raised
        /// </summary>
        public ChangeTypes ChangeTypes { get; }
    }
}
