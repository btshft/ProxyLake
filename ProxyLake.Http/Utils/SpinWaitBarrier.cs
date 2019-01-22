using System;
using System.Threading;

namespace ProxyLake.Http.Utils
{
    internal static class SpinWaitBarrier
    {
        private static readonly object Lock = new object();
        private static SpinWait _wait = new SpinWait();

        public static IDisposable Create()
        {
            return new WaitBarrier();
        }

        private class WaitBarrier : IDisposable
        {
            public WaitBarrier()
            {
                if (Monitor.TryEnter(Lock)) 
                    return;
                
                _wait.SpinOnce();
            }
            
            public void Dispose()
            {
                if (!Monitor.IsEntered(Lock))
                    return;
                
                _wait.Reset();
                Monitor.Exit(Lock);
            }
        }
    }
}