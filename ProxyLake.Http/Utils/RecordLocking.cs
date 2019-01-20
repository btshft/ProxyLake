using System;
using System.Collections.Generic;
using System.Threading;

namespace ProxyLake.Http.Utils
{
    internal static class RecordLocking
    {
        private static readonly object OuterLock = new object();
        private static readonly List<RecordLock> RecordLocks = new List<RecordLock>();

        public static IDisposable AcquireLock(Guid recordId)
        {
            return new RecordLock(recordId);
        }
        
        private class RecordLock : IDisposable
        {
            private readonly object _itemLock;
            private readonly Guid _recordId;

            public RecordLock(Guid recordId)
            {
                _recordId = recordId;

                lock (OuterLock)
                {
                    var existingLock = RecordLocks.Find(l => l._recordId == recordId);
                    _itemLock = existingLock == null
                        ? new object()
                        : existingLock._itemLock;
                    
                    RecordLocks.Add(this);
                }
                
                Monitor.Enter(_itemLock);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (Monitor.IsEntered(_itemLock))
                    Monitor.Exit(_itemLock);

                lock (OuterLock)
                {
                    RecordLocks.Remove(this);
                }
            }
        }
    }
}