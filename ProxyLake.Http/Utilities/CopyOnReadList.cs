using System.Collections;
using System.Collections.Generic;

namespace ProxyLake.Http.Utilities
{
    internal class CopyOnReadList<T> : IList<T>
    {
        private static readonly object _lock = new object();
        
        private readonly IList<T> _internalList;

        public CopyOnReadList()
        {
            _internalList = new List<T>();
        }

        public CopyOnReadList(IList<T> original)
        {
            _internalList = original;
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return ShallowCopy().GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ShallowCopy().GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            _internalList.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _internalList.Clear();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return _internalList.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            return _internalList.Remove(item);
        }

        /// <inheritdoc />
        public int Count => _internalList.Count;

        /// <inheritdoc />
        public bool IsReadOnly => _internalList.IsReadOnly;

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return _internalList.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            _internalList.Insert(index, item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        /// <inheritdoc />
        public T this[int index]
        {
            get => _internalList[index];
            set => _internalList[index] = value;
        }

        private List<T> ShallowCopy()
        {
            var newList = new List<T>();
            lock (_lock)
            {
                foreach (var item in _internalList)
                {
                    newList.Add(item);
                }
            }

            return newList;
        }
    }
}