using System;

namespace Multitool.DAL.FileSystem.Events
{
    [Flags]
    public enum ChangeTypes
    {
        DirectoryCreated,
        FileCreated,
        FileDeleted,
        PathDeleted,
        None
    }
}
