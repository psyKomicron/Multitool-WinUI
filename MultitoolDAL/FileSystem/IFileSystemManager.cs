using Multitool.DAL.FileSystem.Events;

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Windows.Foundation;

namespace Multitool.DAL.FileSystem
{
    public interface IFileSystemManager : IProgressNotifier
    {
        /// <summary>
        /// Raised when the one or more items in the cache have changed.
        /// </summary>
        event TypedEventHandler<IFileSystemManager, ChangeEventArgs> Changed;

        /// <summary>
        /// Raised when a cached path is updating.
        /// </summary>
        public event TypedEventHandler<IFileSystemManager, CacheUpdatingEventArgs> CacheUpdating;

        double CacheTimeout { get; set; }

        /// <summary>
        /// <para>
        /// List the content of a directory as a <see cref="IList{T}"/>.
        /// </para>
        /// <para>
        /// Because each directory size is calculated, the task can be 
        /// cancelled with the <paramref name="cancellationToken"/>.</para>
        /// </summary>
        /// <typeparam name="ItemType">Generic param of the <see cref="IList{T}"/></typeparam>
        /// <param name="path">System file path</param>
        /// <param name="cancellationToken">Cancellation token to cancel this method</param>
        /// <param name="list">Collection to add items to</param>
        /// <param name="addDelegate">Delegate to add items to the <paramref name="list"/></param>
        /// <exception cref="System.ArgumentNullException">
        /// If either <paramref name="list"/> or <paramref name="cancellationToken"/> is null/>
        /// </exception>
        Task GetFileSystemEntries<ItemType>(
            string path, IList<ItemType> list,
            AddDelegate<ItemType> addDelegate, CancellationToken cancellationToken) where ItemType : IFileSystemEntry;
        /// <summary>
        /// Get the case sensitive path for the <paramref name="path"/> parameter.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>The "real" path</returns>
        string GetRealPath(string path);
        /// <summary>
        /// Cleans the internal cache.
        /// </summary>
        void Reset();
    }
}