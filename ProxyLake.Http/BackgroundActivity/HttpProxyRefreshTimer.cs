using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utilities;

namespace ProxyLake.Http.BackgroundActivity
{
    internal class HttpProxyRefreshTimer : IDisposable
    {
        private readonly Timer _timer;
        private readonly IList<TrackingProxy> _proxyStates;
        private readonly IHttpProxyRefreshService _proxyRefreshService;
        private readonly HttpProxyClientFactoryOptions _options;
        private readonly IHttpProxyPool _httpProxyPool;
        private readonly ILogger _logger;

        public HttpProxyRefreshTimer(
            IOptions<HttpProxyClientFactoryOptions> options, 
            IHttpProxyRefreshService proxyRefreshService, 
            IList<TrackingProxy> proxyStates, 
            IHttpProxyPool httpProxyPool,
            IHttpProxyLoggerFactory loggerFactory)
        {
            _options = options.Value;
            _proxyRefreshService = proxyRefreshService;
            _proxyStates = proxyStates;
            _httpProxyPool = httpProxyPool;
            _logger = loggerFactory.CreateLogger(typeof(HttpProxyRefreshTimer));
            _timer = NonCapturingTimer.Create(
                Tick, this, options.Value.ProxyRefreshDueTime, options.Value.ProxyRefreshPeriod);
        }

        internal static void Tick(object state)
        {
            var timer = (HttpProxyRefreshTimer) state;
            var logger = timer._logger;
            
            if (timer._proxyStates.Count < timer._options.AliveProxiesLowerLimit)
            {
                try
                {
                    var aliveProxies = timer._proxyStates.Select(p => p.Proxy).ToArray();
                    var newProxies = timer._proxyRefreshService.RefreshProxies(
                        aliveProxies, maxProxiesCount: timer._options.AliveProxiesUpperLimit)
                        .ToList();

                    if (newProxies.Count == 0)
                    {
                        logger.LogDebug($"No proxies returned from refresh service '{timer._proxyRefreshService.GetType().Name}'");
                        // Log?
                        return;
                    }

                    logger.LogDebug($"'{newProxies.Count}' proxies returned from refresh service '{timer._proxyRefreshService.GetType().Name}'");
                    timer._proxyStates.Clear();
                    
                    foreach (var proxy in newProxies)
                    {
                        timer._proxyStates.Add(
                            new TrackingProxy(
                                new RentedProxy(proxy, timer._httpProxyPool)));
                    }

                }
                catch (Exception e)
                {
                    logger.LogError(e,
                        $"Proxy refresh failed with service '{timer._proxyRefreshService.GetType().Name}'");
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}