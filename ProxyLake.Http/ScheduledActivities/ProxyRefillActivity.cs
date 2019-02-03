using System;
using System.Collections.Concurrent;
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
        private readonly IHttpProxyFactory _proxyFactory;
        private readonly Func<IReadOnlyCollection<IHttpProxy>> _aliveProxyAccessor;
        private readonly Action<IHttpProxy> _proxyInsertCallback;
        private readonly HttpProxyClientFactoryOptions _options;
        
        public ProxyRefillActivity(
            TimeSpan period,
            HttpProxyClientFactoryOptions options,
            IHttpProxyLoggerFactory loggerFactory,
            Func<IReadOnlyCollection<IHttpProxy>> aliveProxyAccessor,
            Action<IHttpProxy> proxyInsertCallback, 
            IHttpProxyRefill proxyRefill, 
            IHttpProxyFactory proxyFactory) 
            : base(period, period)
        {
            _logger = loggerFactory.CreateLogger(typeof(ProxyRefillActivity));
            _options = options;
            _aliveProxyAccessor = aliveProxyAccessor;
            _proxyInsertCallback = proxyInsertCallback;
            _proxyRefill = proxyRefill;
            _proxyFactory = proxyFactory;
        }

        /// <inheritdoc />
        protected override void Execute(CancellationToken cancellation)
        {
            var exceptions = new ConcurrentQueue<Exception>();
            var proxiesAdded = 0;
            var aliveProxiesCount = 0;
            
            try
            {
                var aliveProxies = _aliveProxyAccessor();
                aliveProxiesCount = aliveProxies.Count;
                
                if (aliveProxies.Count <= _options.MinProxyCount)
                {
                    var requestProxiesCount = _options.MaxProxyCount - aliveProxies.Count;
                    var proxyDefinitions = _proxyRefill.GetProxies(aliveProxies, maxProxiesCount: requestProxiesCount,
                        cancellation);

                    if (proxyDefinitions == null || proxyDefinitions.Count == 0)
                    {
                        _logger.LogWarning("Proxy refill returned null or empty set. Cancelling refill cycle...");
                        return;
                    }

                    if (proxyDefinitions.Count > _options.MaxProxyCount)
                        proxyDefinitions = proxyDefinitions.Take(_options.MaxProxyCount).ToArray();

                    foreach (var definition in proxyDefinitions)
                    {
                        try
                        {
                            _proxyInsertCallback(_proxyFactory.CreateProxy(definition));
                            proxiesAdded++;
                        }
                        catch (Exception e)
                        {
                            exceptions.Enqueue(new Exception($"Unable to add proxy '{definition.Host}:{definition.Port}'", e));
                        }
                    }
                    
                    if (exceptions.Count > 0)
                        throw new AggregateException(exceptions);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Refill cycle failed");
            }
            
            _logger.LogInformation($"[Refill cycle]: {Environment.NewLine}" +
                                   $"added: {proxiesAdded}; {Environment.NewLine}" +
                                   $"failed_to_add: {exceptions.Count} {Environment.NewLine}" +
                                   $"alive_before: {aliveProxiesCount} {Environment.NewLine}" +
                                   $"alive_after: {_aliveProxyAccessor().Count} {Environment.NewLine}" +
                                   $"min_alive_count_inclusive: {_options.MinProxyCount} {Environment.NewLine}" +
                                   $"max_alive_count_inclusive: {_options.MaxProxyCount}");
        }
    }
}