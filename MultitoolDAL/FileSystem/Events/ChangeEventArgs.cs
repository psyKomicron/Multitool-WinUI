using System;

#nullable enable
namespace Multitool.Data.FileSystem.Events
{
    /// <summary>
    /// Provides data for file system changes events.
    /// </summary>
    public class ChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ChangeEventArgs() : base()
        {
            Entry = null;
            ChangeTypes = ChangeTypes.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="changeTypes">Change types</param>
        public ChangeEventArgs(FileSystemEntry entry, ChangeTypes changeTypes) : base()
        {
            Entry = entry;
            ChangeTypes = changeTypes;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="changeTypes">Change types</param>
        public ChangeEventArgs(ChangeTypes changeTypes) : base()
        {
            ChangeTypes = changeTypes;
        }

        internal ChangeEventArgs(CacheChangedEventArgs eventArgs) : this(eventArgs.Entry, eventArgs.ChangeType) { }

        /// <summary>
        /// Entry associated with the event
        /// </summary>
        public IFileSystemEntry? Entry { get; }

        /// <summary>
        /// Why this event was raised
        /// </summary>
        public ChangeTypes ChangeTypes { get; }
    }
}
