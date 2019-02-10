using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.ScheduledActivities
{
    internal class HandlersCleanupActivity : ScheduledActivity
    {
        private readonly ConcurrentQueue<DeadHandlerReference> _handlers;
        
        public HandlersCleanupActivity(
            TimeSpan period,
            ConcurrentQueue<DeadHandlerReference> handlers,
            IHttpProxyLoggerFactory loggerFactory) 
            : base(period, loggerFactory.CreateLogger(typeof(HandlersCleanupActivity)))
        {
            _handlers = handlers;
        }

        /// <inheritdoc />
        protected override Task ExecuteAsync(CancellationToken cancellation)
        {
            int collected = 0,
                handlersCount = _handlers.Count;

            for (var i = 0; i < handlersCount; i++)
            {
                if (!_handlers.TryDequeue(out var deadHandler))
                {
                    Logger.LogDebug("Unable to get handler from queue");
                }

                var proxyId = deadHandler.Proxy?.Id;

                if (!deadHandler.IsCollectedByGC)
                {
                    _handlers.Enqueue(deadHandler);
                    Logger.LogDebug(
                        $"Unable to collect handler with proxy '{proxyId}' - it's still in use");
                }
                else
                {
                    try
                    {
                        deadHandler.Inner.Dispose();
                        collected++;
                        
                        Logger.LogDebug($"Proxy with id '{proxyId}' collected by GC. Related handler was disposed.");
                    }
                    catch (Exception e)
                    {
                        Logger.LogDebug(
                            $"Unable to collect handler with proxy '{proxyId}' - exception occured: {e}");
                        
                    }
                }
            }

            Logger.LogInformation($"[Cleanup cycle] dead ref count: {handlersCount}; GC collected: {collected}; ");
            
            return Task.CompletedTask;
        }
    }
}