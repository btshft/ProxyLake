using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.ScheduledActivities
{
    internal class ProxyRefillActivity : ScheduledActivity
    {
        private readonly ILogger _logger;
        private readonly IHttpProxyRefill _proxyRefill;
        private readonly Func<IReadOnlyCollection<IHttpProxy>> _aliveProxyAccessor;
        private readonly Action<IHttpProxy> _proxyInsertCallback;
        private readonly HttpProxyClientFactoryOptions _options;
        
        public ProxyRefillActivity(
            TimeSpan period,
            HttpProxyClientFactoryOptions options,
            IHttpProxyLoggerFactory loggerFactory,
            Func<IReadOnlyCollection<IHttpProxy>> aliveProxyAccessor,
            Action<IHttpProxy> proxyInsertCallback, 
            IHttpProxyRefill proxyRefill) 
            : base(period, period)
        {
            _logger = loggerFactory.CreateLogger(typeof(ProxyRefillActivity));
            _options = options;
            _aliveProxyAccessor = aliveProxyAccessor;
            _proxyInsertCallback = proxyInsertCallback;
            _proxyRefill = proxyRefill;
        }

        /// <inheritdoc />
        protected override void Execute(CancellationToken cancellation)
        {
            try
            {
                var aliveProxies = _aliveProxyAccessor();

                if (aliveProxies.Count <= _options.MinProxyCount)
                {
                    var requestProxiesCount = _options.MaxProxyCount - aliveProxies.Count;
                    var newProxies = _proxyRefill.GetProxies(aliveProxies, maxProxiesCount: requestProxiesCount,
                        cancellation);
                    
                    if (newProxies == null)
                        return;

                    if (newProxies.Count > _options.MaxProxyCount)
                        newProxies = newProxies.Take(_options.MaxProxyCount).ToArray();

                    foreach (var proxy in newProxies)
                        _proxyInsertCallback(proxy);
                }
            }
            catch (Exception e)
            {
                // TODO: Logging
            }
        }
    }
}