using System.Threading;

namespace ProxyLake.Http.ScheduledActivities
{
    internal sealed class NullScheduledActivity : IScheduledActivity
    {
        public bool IsRunning { get; } = true;
        public void Start(CancellationToken cancellation) { }

        public void Stop() { }

        public void Dispose()
        { }
    }
}