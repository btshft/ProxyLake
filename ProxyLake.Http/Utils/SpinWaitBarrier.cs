using System;
using System.Threading;

namespace ProxyLake.Http.Utils
{
    internal static class SpinWaitBarrier
    {
        private static readonly object Lock = new object();
        private static int _failedThreads = 0;

        public static IDisposable Create(int spinWaitMultiplier)
        {
            return new WaitBarrier(spinWaitMultiplier);
        }

        private class WaitBarrier : IDisposable
        {           
            public WaitBarrier(int multiplier)
            {
                if (multiplier < 1)
                    multiplier = 1;
                
                if (Monitor.TryEnter(Lock)) 
                    return;
                
                Interlocked.Increment(ref _failedThreads);
                Thread.SpinWait(_failedThreads * multiplier);
                Interlocked.Decrement(ref _failedThreads);
            }
            
            public void Dispose()
            {
                if (Monitor.IsEntered(Lock))
                {
                    Monitor.Exit(Lock);
                    Interlocked.Exchange(ref _failedThreads, 0);
                }
            }
        }
    }
}