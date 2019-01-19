using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http
{
    internal class RoundRobinProxyRotationService : IHttpProxyRotationService
    {
        private readonly IHttpProxyPool _proxyPool;
        private readonly ILogger _logger;

        public RoundRobinProxyRotationService(IHttpProxyPool proxyPool, IHttpProxyLoggerFactory loggerFactory)
        {
            _proxyPool = proxyPool;
            _logger = loggerFactory.CreateLogger(typeof(RoundRobinProxyRotationService));
        }

        /// <inheritdoc />
        public bool IsRotationRequired(IHttpProxy proxy)
        {
            if (proxy == null)
                throw new ArgumentNullException(nameof(proxy));

            return true;
        }

        /// <inheritdoc />
        public bool TryRotate(IHttpProxy current, CancellationToken cancellationToken, out IRentedHttpProxy newProxy)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            newProxy = _proxyPool.RentNext(current, cancellationToken);
            if (newProxy == null || newProxy.Id == current.Id)
            {
                if (newProxy == null)
                    _logger.LogWarning("Proxy pool returned null. Proxy rotation failed.");
                else 
                    _logger.LogWarning($"Proxy pool returned same proxy with id '{newProxy.Id}'. Proxy rotation failed.");
                
                // log
                return false;
            }

            return true;
        }
    }
}