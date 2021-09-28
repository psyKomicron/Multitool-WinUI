using System.Collections.Generic;

namespace Multitool.DAL
{
    public delegate void AddDelegate<ItemType>(IList<ItemType> items, IFileSystemEntry item) where ItemType : IFileSystemEntry;
}
