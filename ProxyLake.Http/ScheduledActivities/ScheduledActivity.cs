using System;
using System.Threading;

namespace ProxyLake.Http.ScheduledActivities
{
    internal abstract class ScheduledActivity : IScheduledActivity, IDisposable
    {
        private readonly object _lock;
        private readonly TimeSpan _period;
        private readonly TimeSpan _dueTime;
        
        private Timer _taskTimer;
        private bool _isRunning;
        private CancellationToken _cancellationToken;

        public bool IsRunning => _isRunning;

        protected ScheduledActivity(TimeSpan period)
            : this(TimeSpan.Zero, period)
        { }
        
        protected ScheduledActivity(TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            _dueTime = dueTime;
            _lock = new object();
        }

        public void Start(CancellationToken cancellation)
        {
            lock (_lock)
            {
                if (Volatile.Read(ref _isRunning))
                    return;

                _cancellationToken = cancellation;
                _taskTimer = CreateTimer(TimerTick, this, _dueTime, _period);
                Volatile.Write(ref _isRunning, true);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (!Volatile.Read(ref _isRunning))
                    return;

                _taskTimer?.Dispose();
                _taskTimer = null;
            }
        }

        protected abstract void Execute(CancellationToken cancellation);

        private static void TimerTick(object state)
        {
            var activity = (ScheduledActivity) state;
            if (activity._cancellationToken.IsCancellationRequested)
            {
                activity._taskTimer.Dispose();
                activity._taskTimer = null;
            }
            else
            {
                activity.Execute(activity._cancellationToken);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _taskTimer?.Dispose();
        }
        
        private static Timer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer
            var restoreFlow = false;
            try
            {
                if (ExecutionContext.IsFlowSuppressed()) 
                    return new Timer(callback, state, dueTime, period);
                
                ExecutionContext.SuppressFlow();
                restoreFlow = true;

                return new Timer(callback, state, dueTime, period);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        } 
    }
}