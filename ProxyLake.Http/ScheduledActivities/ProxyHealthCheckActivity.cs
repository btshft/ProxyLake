using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.ScheduledActivities
{
    internal class ProxyHealthCheckActivity : ScheduledActivity
    {
        private readonly IHttpProxyHealthCheckFeature _healthCheck;
        private readonly Func<HttpProxyHandlerState, bool> _removeHandler;
        private readonly Func<IReadOnlyCollection<HttpProxyHandlerState>> _handlerAccessor;
        private readonly Action<DeadHandlerReference> _deadHandlerInsertion;

        public ProxyHealthCheckActivity(
            TimeSpan period,
            IHttpProxyLoggerFactory loggerFactory, 
            IHttpProxyHealthCheckFeature healthCheck, 
            Func<HttpProxyHandlerState, bool> removeHandler, 
            Func<IReadOnlyCollection<HttpProxyHandlerState>> handlerAccessor, 
            Action<DeadHandlerReference> deadHandlerInsertion) 
            : base(period, period, loggerFactory.CreateLogger(typeof(ProxyHealthCheckActivity)))
        {
            _healthCheck = healthCheck;
            _removeHandler = removeHandler;
            _handlerAccessor = handlerAccessor;
            _deadHandlerInsertion = deadHandlerInsertion;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken cancellation)
        {
            var handlerStates = _handlerAccessor();
            
            if (handlerStates.Count == 0)
                return;

            int removed = 0, total = handlerStates.Count;
            foreach (var state in handlerStates)
            {
                try
                {
                    var isAlive = await _healthCheck.IsAliveAsync(state.ProxyState.Proxy, cancellation)
                        .ConfigureAwait(continueOnCapturedContext: true);
                    
                    if (isAlive) 
                        continue;
                    
                    if (_removeHandler(state))
                    {
                        _deadHandlerInsertion(new DeadHandlerReference(state));
                        removed++;
                    }
                    else
                    {
                        Logger.LogDebug(
                            $"Unable to remove handler with proxy '{state.ProxyState.Proxy.Id}' from active handlers");
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Error occured on proxy '{state.ProxyState.Proxy.Id}' health check");
                }
            }
            
            Logger.LogInformation($"[Health check cycle]: total checked: {total}; removed: {removed}");
        }
    }
}