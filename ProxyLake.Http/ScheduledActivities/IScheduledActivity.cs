using System;
using System.Threading;

namespace ProxyLake.Http.ScheduledActivities
{
    internal interface IScheduledActivity : IDisposable
    {
        bool IsRunning { get; }
        void Start(CancellationToken cancellation);
        void Stop();
    }
}