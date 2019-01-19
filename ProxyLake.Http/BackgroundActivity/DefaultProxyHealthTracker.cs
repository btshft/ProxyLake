using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utilities;

namespace ProxyLake.Http.BackgroundActivity
{
    internal class DefaultProxyHealthTracker : IHttpProxyHealthTracker
    {
        private readonly IHttpProxyHealthChecker[] _healthCheckers;
        private readonly ILogger _logger;

        public DefaultProxyHealthTracker(
            IHttpProxyHealthCheckerProvider provider,
            IHttpProxyLoggerFactory loggerFactory)
        {
            _healthCheckers = provider.GetHealthCheckers().ToArray();
            _logger = loggerFactory.CreateLogger(typeof(DefaultProxyHealthTracker));
        }

        /// <inheritdoc />
        public void ProcessBatch(IList<TrackingProxy> proxies)
        {
            if (proxies == null || proxies.Count == 0 || _healthCheckers == null || _healthCheckers.Length == 0)
                return;
            
            var proxyStatesCopy = new List<TrackingProxy>(proxies);
            
            foreach (var proxyState in proxyStatesCopy)
            {
                using (RecordLocking.AcquireLock(proxyState.Proxy.Id))
                {
                    foreach (var healthChecker in _healthCheckers)
                    {
                        try
                        {
                            if (!healthChecker.IsAlive(proxyState.Proxy))
                            {
                                if (proxies.Remove(proxyState))
                                {
                                    // Log
                                }                
                            }

                            proxyState.LastHealthCheck = DateTime.UtcNow;
                        }
                        catch (Exception e)
                        {
                            if (proxies.Remove(proxyState))
                            {
                                // Log
                            }
                        }
                    }
                }
            }
        }
    }
}