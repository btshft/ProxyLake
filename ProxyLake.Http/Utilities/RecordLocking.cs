using System;
using System.Collections.Generic;
using System.Threading;

namespace ProxyLake.Http.Utilities
{
    internal static class RecordLocking
    {
        private static readonly object OuterLock = new object();
        private static readonly List<RecordLock> ProxyLocks = new List<RecordLock>();

        public static IDisposable AcquireLock(Guid proxyId)
        {
            return new RecordLock(proxyId);
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
                    var existingLock = ProxyLocks.Find(l => l._recordId == recordId);
                    _itemLock = existingLock == null
                        ? new object()
                        : existingLock._itemLock;
                    
                    ProxyLocks.Add(this);
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
                    ProxyLocks.Remove(this);
                }
            }
        }
    }
}