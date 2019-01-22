using System.Threading;

namespace ProxyLake.Http.ScheduledActivities
{
    internal interface IScheduledActivity
    {
        bool IsRunning { get; }
        void Start(CancellationToken cancellation);
        void Stop();
    }
}