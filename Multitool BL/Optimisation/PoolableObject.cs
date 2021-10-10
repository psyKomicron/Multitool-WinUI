using System;

namespace Multitool.Optimisation
{
    /// <summary>
    /// Allows for objects to be reused when they are not longer used.
    /// Mainly used for event data.
    /// </summary>
    public class PoolableObject : EventArgs
    {
        private volatile bool _inUse;

        /// <summary>
        /// Tells if the <see cref="IPoolableObject"/> is in use and thus cannot be used by the pool.
        /// </summary>
        public bool InUse
        {
            get => _inUse;
            set
            {
                _inUse = value;
                if (!_inUse)
                {
                    Free?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Fired when the object is free to be reused by the <see cref="ObjectPool{T}"/>.
        /// </summary>
        public event FreeObjectEventHandler Free;
    }
}
