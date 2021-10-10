using System;

namespace Multitool.DAL.FileSystem.Events
{
    [Flags]
    public enum ChangeTypes
    {
        FileCreated,
        FileDeleted,
        FileChanged,
        FileRenamed,
        PathDeleted,
        All = FileCreated | FileDeleted | FileChanged | FileRenamed | PathDeleted
    }
}
