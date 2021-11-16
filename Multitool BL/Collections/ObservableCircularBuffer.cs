using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Collections
{
    public class ObservableCircularBuffer<T> : INotifyCollectionChanged, IList
    {
        private readonly T[] buffer;
        private bool full;
        private int head;
        private int tail;

        public ObservableCircularBuffer(int baseSize)
        {
            buffer = new T[baseSize];
        }

        #region properties
        /// <inheritdoc/>
        object IList.this[int index]
        {
            get => buffer[GetRealIndex(index)];
            set => buffer[GetRealIndex(index)] = value is T tValue
                    ? tValue
                    : throw new ArrayTypeMismatchException($"Type mismatch : {value.GetType()} vs {typeof(T)}");
        }

        public T this[int index]
        {
            get => buffer[GetRealIndex(index)];
            set => buffer[GetRealIndex(index)] = value;
        }

        /// <inheritdoc/>
        public bool IsEmpty { get; private set; }

        /// <inheritdoc/>
        public bool IsFixedSize => true;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public int Count => head;

        /// <inheritdoc/>
        public bool IsSynchronized => false;

        /// <inheritdoc/>
        public object SyncRoot => null;
        #endregion

        #region events
        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region interface implementations
        /// <inheritdoc/>
        public int Add(object value)
        {
            if (value is T tValue)
            {
                Add(tValue);
                return -1;
            }
            else
            {
                throw new ArrayTypeMismatchException();
            }
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            int insertIndex = head;
            buffer[head] = item;
            if (!full && head + 1 == buffer.Length)
            {
                full = true;
            }
            else if (full)
            {
                tail = (tail + 1) % buffer.Length;
            }
            head = (head + 1) % buffer.Length;
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc/>
        public void Clear()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = default;
            }
            head = tail = 0;
            full = false;
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc/>
        public bool Contains(object value)
        {
            if (IsEmpty)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].Equals(buffer[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void CopyTo(Array array, int index)
        {
            buffer.CopyTo(array, index);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            buffer.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <inheritdoc/>
        public int IndexOf(object value)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer.Equals(buffer[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (buffer[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">The insertion is not supported in this collection</exception>
        public void Insert(int index, object value)
        {
            throw new NotSupportedException("Insertion is not supported");
        }

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">The insertion is not supported in this collection</exception>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Insertion is not supported");
        }

        /// <inheritdoc/>
        public void Remove(object value)
        {
            if (value is T tValue)
            {
                _ = Remove(tValue);
            }
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (buffer[i].Equals(item))
                {
                    T removed = buffer[i];
                    buffer[i] = default;
                    CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, removed));
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            T removed = buffer[GetRealIndex(index)];
            buffer[GetRealIndex(index)] = default;
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, removed, index));
        }
        #endregion

        #region private
        private int GetRealIndex(int index)
        {
            return (tail + index) % buffer.Length;
        }

        private int GetCount()
        {
            return buffer.GetUpperBound(1);
        }
        #endregion

        #region enumerator
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ObservableCircularBuffer<T> parent;
            private int max;
            private int iterator;
            private bool disposedValue;

            internal Enumerator(ObservableCircularBuffer<T> parent)
            {
                this.parent = parent;
                iterator = 0;
                max = parent.buffer.Length;
                disposedValue = false;
                Current = default;
            }

            /// <inheritdoc/>
            public T Current { get; private set; }

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            /// <inheritdoc/>
            public void Dispose()
            {
                if (!disposedValue)
                {
                    disposedValue = true;
                }
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                CheckIfDisposed();
                if (iterator == max)
                {
                    Current = default;
                    return false;
                }
                else
                {
                    Current = parent[iterator];
                    iterator++;
                    return true;
                }
            }

            /// <inheritdoc/>
            public void Reset()
            {
                CheckIfDisposed();
                iterator = 0;
                Current = default;
            }

            private void CheckIfDisposed()
            {
                if (disposedValue)
                {
                    throw new ObjectDisposedException(nameof(Enumerator));
                }
            }
        }
        #endregion
    }
}
