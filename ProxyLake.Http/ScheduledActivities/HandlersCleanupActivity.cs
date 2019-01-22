using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.ScheduledActivities
{
    internal class HandlersCleanupActivity : ScheduledActivity
    {
        private readonly ConcurrentQueue<DeadHandlerReference> _handlers;
        private readonly ILogger _logger;

        public HandlersCleanupActivity(
            TimeSpan period,
            ConcurrentQueue<DeadHandlerReference> handlers,
            IHttpProxyLoggerFactory loggerFactory) 
            : base(period)
        {
            _handlers = handlers;
            _logger = loggerFactory.CreateLogger(typeof(HandlersCleanupActivity));
        }

        /// <inheritdoc />
        protected override void Execute(CancellationToken cancellation)
        {
            int collected = 0,
                handlersCount = _handlers.Count;

            GC.Collect();
            
            for (var i = 0; i < handlersCount; i++)
            {
                if (!_handlers.TryDequeue(out var deadHandler))
                {
                    _logger.LogDebug("Unable to get handler from queue");
                }

                var proxyId = deadHandler.Proxy?.Id;

                if (!deadHandler.IsCollectedByGC)
                {
                    _handlers.Enqueue(deadHandler);
                    _logger.LogDebug(
                        $"Unable to collect handler with proxy '{proxyId}' - it's still in use");
                }
                else
                {
                    try
                    {
                        deadHandler.Inner.Dispose();
                        collected++;
                        
                        _logger.LogDebug($"Proxy with id '{proxyId}' collected by GC. Related handler was disposed.");
                    }
                    catch (Exception e)
                    {
                        // log
                        _logger.LogDebug(
                            $"Unable to collect handler with proxy '{proxyId}' - exception occured: {e}");
                        
                    }
                }
            }

            _logger.LogInformation($"[Cleanup cycle] dead ref count: {handlersCount}; GC collected: {collected}; ");  
        }
    }
}