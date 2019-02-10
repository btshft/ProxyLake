using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProxyLake.Http.ScheduledActivities
{
    internal abstract class ScheduledActivity : IScheduledActivity
    {
        protected readonly ILogger Logger;
        protected readonly TimeSpan Period;
        protected readonly TimeSpan DueTime;
        
        private readonly object _lock;
 
        private Timer _taskTimer;
        private bool _isRunning;
        private CancellationToken _cancellationToken;

        public bool IsRunning => _isRunning;

        protected ScheduledActivity(TimeSpan period, ILogger logger)
            : this(TimeSpan.Zero, period, logger)
        { }
        
        protected ScheduledActivity(TimeSpan dueTime, TimeSpan period, ILogger logger)
        {
            Period = period;
            Logger = logger;
            DueTime = dueTime;
            _lock = new object();
        }

        public void Start(CancellationToken cancellation)
        {
            lock (_lock)
            {
                if (Volatile.Read(ref _isRunning))
                    return;

                _cancellationToken = cancellation;
                _taskTimer = CreateTimer(TimerTick, this, DueTime, Period);
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

        protected abstract Task ExecuteAsync(CancellationToken cancellation);

        private static async void TimerTick(object state)
        {
            var activity = (ScheduledActivity) state;
            if (activity._cancellationToken.IsCancellationRequested)
            {
                activity._taskTimer.Dispose();
                activity._taskTimer = null;
            }
            else
            {
                try
                {
                    await activity.ExecuteAsync(activity._cancellationToken)
                        .ConfigureAwait(continueOnCapturedContext: true);
                }
                catch (Exception e)
                {
                    activity.Logger.LogError(e, $"[Activity '{activity.GetType().Name}' fatal exception]: {e.Message}");
                    activity._taskTimer.Dispose();
                    activity._taskTimer = null;
                }
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