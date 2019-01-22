using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProxyLake.Http.Utils
{
    internal sealed class CopyOnWriteCollection<T> : ICollection<T>
    {
        private readonly object _sync;
        private List<T> _writeCollection, _readCollection;

        internal IReadOnlyCollection<T> ReadCollection
        {
            get
            {
                lock (_sync)
                {
                    if (_readCollection == null)
                        _readCollection = new List<T>(_writeCollection);

                    return _readCollection;
                }
            }
        }
        
        public CopyOnWriteCollection()
        {
            _writeCollection = new List<T>();
            _readCollection = null;
            _sync = new object();
        }
        
        public CopyOnWriteCollection(IReadOnlyCollection<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _writeCollection = new List<T>(source);
            _readCollection = null;
            _sync = new object();
        }

        /// <inheritdoc />
        public int Count => ReadCollection.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;
        
        /// <inheritdoc />
        public void Add(T item)
        {
            lock (_sync)
            {
                CopyWriteList();
                _writeCollection.Add(item);
            }
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_sync)
            {
                CopyWriteList();
                _writeCollection.Clear();
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return ReadCollection.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            ((List<T>)ReadCollection).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            lock (_sync)
            {
                var exists = FindIndex(item) != -1;
                if (!exists)
                    return false;
                
                CopyWriteList();
                _writeCollection.Remove(item);

                return true;
            }
        }
        
        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return ReadCollection.GetEnumerator();
        }
        
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CopyWriteList()
        {
            if (_readCollection != null)
            {
                _writeCollection = new List<T>(_writeCollection);
                _readCollection = null;
            }
        }

        private int FindIndex(T item)
        {
            // ReSharper disable InconsistentlySynchronizedField
            // lock should be acquired outside
            for (var i = 0; i < _writeCollection.Count; i++)
            {
                if (Equals(item, _writeCollection[i]))
                    return i;
            }
            // ReSharper restore InconsistentlySynchronizedField

            return -1;
        }
    }
    

}