using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utilities;

namespace ProxyLake.Http.BackgroundActivity
{
    internal class HttpProxyHealthCheckTimer : IDisposable
    {
        private readonly Timer _timer;
        private readonly IList<TrackingProxy> _proxyStates;
        private readonly IHttpProxyHealthTracker _tracker;
        private readonly ILogger _logger;

        public HttpProxyHealthCheckTimer(
            IOptions<HttpProxyClientFactoryOptions> options, 
            IHttpProxyHealthTracker tracker, 
            IList<TrackingProxy> proxyStates,
            IHttpProxyLoggerFactory loggerFactory)
        {
            _tracker = tracker;
            _proxyStates = proxyStates;
            _timer = NonCapturingTimer.Create(
                Tick, this, options.Value.HealthCheckDueTime, options.Value.HealthCheckPeriod);
            _logger = loggerFactory.CreateLogger(typeof(HttpProxyHealthCheckTimer));
        }

        internal static void Tick(object state)
        {
            var timer = (HttpProxyHealthCheckTimer) state;
            var logger = timer._logger;
            try
            {
                timer._tracker.ProcessBatch(timer._proxyStates);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Proxies processing failed with tracker '{timer._tracker.GetType().Name}'");
                // Log
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}