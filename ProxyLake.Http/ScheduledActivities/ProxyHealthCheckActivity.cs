using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http.ScheduledActivities
{
    internal class ProxyHealthCheckActivity : ScheduledActivity
    {
        private readonly CopyOnWriteCollection<HttpProxyHandlerState> _handlerStates;
        private readonly ConcurrentQueue<DeadHandlerReference> _deadHandlers;
        private readonly ILogger _logger;
        private readonly IHttpProxyHealthCheckFeature _healthCheck;

        public ProxyHealthCheckActivity(
            TimeSpan period,
            CopyOnWriteCollection<HttpProxyHandlerState> handlerStates,
            ConcurrentQueue<DeadHandlerReference> deadHandlers,
            IHttpProxyLoggerFactory loggerFactory, 
            IHttpProxyHealthCheckFeature healthCheck) : base(period, period)
        {
            _handlerStates = handlerStates;
            _logger = loggerFactory.CreateLogger(typeof(ProxyHealthCheckActivity));
            _deadHandlers = deadHandlers;
            _healthCheck = healthCheck;
        }

        /// <inheritdoc />
        protected override void Execute(CancellationToken cancellation)
        {
            if (_handlerStates.Count == 0)
                return;

            int removed = 0, total = _handlerStates.Count;
            foreach (var state in _handlerStates)
            {
                try
                {
                    if (_healthCheck.IsAlive(state.ProxyState.Proxy, cancellation)) 
                        continue;
                    
                    if (_handlerStates.Remove(state))
                    {
                        _deadHandlers.Enqueue(new DeadHandlerReference(state));
                        removed++;
                    }
                    else
                    {
                        _logger.LogDebug(
                            $"Unable to remove handler with proxy '{state.ProxyState.Proxy.Id}' from active handlers");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error occured on proxy '{state.ProxyState.Proxy.Id}' health check");
                }
            }
            
            _logger.LogInformation($"[Health check cycle]: total checked: {total}; removed: {removed}");
        }
    }
}