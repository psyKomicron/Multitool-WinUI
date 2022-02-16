using System.Collections.Generic;

namespace Multitool.Data
{
    /// <summary>
    /// Delegate.
    /// </summary>
    /// <typeparam name="ItemType">Type of the item's list</typeparam>
    /// <param name="items">Reference to a list of items for the delegate</param>
    /// <param name="item">The new item to add</param>
    public delegate void AddDelegate<ItemType>(IList<ItemType> items, IFileSystemEntry item) where ItemType : IFileSystemEntry;
}
