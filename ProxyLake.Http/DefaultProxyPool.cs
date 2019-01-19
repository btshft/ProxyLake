using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.BackgroundActivity;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utilities;

namespace ProxyLake.Http
{
    internal class DefaultProxyPool : IHttpProxyPool, IDisposable
    {
        private readonly CopyOnReadList<TrackingProxy> _proxyStates;   
        private readonly HttpProxyHealthCheckTimer _httpProxyHealthCheckTimer;
        private readonly HttpProxyRefreshTimer _refreshTimer;
        private readonly ManualResetEventSlim _proxyAvailableResetEvent;
        private readonly IHttpProxyHealthChecker[] _healthCheckers;
        private readonly ILogger _logger;

        public DefaultProxyPool(
            IHttpProxyHealthTracker healthTracker, 
            IHttpProxyFactory proxyFactory, 
            IHttpProxyDefinitionProvider definitionProvider,
            IHttpProxyHealthCheckerProvider healthCheckerProvider,
            IHttpProxyLoggerFactory loggerFactory,
            IOptions<HttpProxyClientFactoryOptions> options, 
            IHttpProxyRefreshService proxyRefreshService)
        {
            _proxyAvailableResetEvent = new ManualResetEventSlim(initialState: true, spinCount: 10);
            _proxyStates = new CopyOnReadList<TrackingProxy>(definitionProvider.GetProxyDefinitions()
                .Select(proxyFactory.CreateProxy)
                .Select(p => new TrackingProxy(new RentedProxy(p,this)))
                .ToList());

            if (_proxyStates.Count == 0)
            {
                throw new Exception("Unable to create proxy pool. No proxy definitions were found.");
            }

            _healthCheckers = healthCheckerProvider.GetHealthCheckers().ToArray();
            _logger = loggerFactory.CreateLogger(typeof(DefaultProxyPool));

            if (_healthCheckers.Length > 0)
            {
                _logger.LogDebug($"Creating health check timer with tracker '{healthTracker.GetType().Name}'");
                _httpProxyHealthCheckTimer = new HttpProxyHealthCheckTimer(options, healthTracker, _proxyStates, loggerFactory);
            }

            if (proxyRefreshService != null && !(proxyRefreshService is NullProxyRefreshService))
            {
                _logger.LogDebug($"Creating proxy refresh timer with service '{proxyRefreshService.GetType().Name}'");
                _refreshTimer = new HttpProxyRefreshTimer(options, proxyRefreshService, _proxyStates, this, loggerFactory);
            }
        }
        
        /// <inheritdoc />
        public IRentedHttpProxy Rent(CancellationToken cancellation)
        {
            return RentNext(current: null, cancellation);
        }

        /// <inheritdoc />
        public IRentedHttpProxy RentNext(IHttpProxy current, CancellationToken cancellation)
        {
            TrackingProxy GetNextReference()
            {
                var proxyStatesShallowCopy = _proxyStates.ToList();
                
                if (current == null)
                    return proxyStatesShallowCopy.FirstOrDefault(p => !p.IsRented);

                var index = proxyStatesShallowCopy.FindIndex(p => p.Proxy.Id == current.Id);
                // Last element - try from start
                if (index + 1 == proxyStatesShallowCopy.Count)
                    return proxyStatesShallowCopy.FirstOrDefault(p => !p.IsRented);

                var reference = proxyStatesShallowCopy.Skip(index + 1).FirstOrDefault(p => !p.IsRented);
                // No free element after current - try from start
                return reference ?? proxyStatesShallowCopy.FirstOrDefault(p => !p.IsRented);
            }
            
            _proxyAvailableResetEvent.Reset();

            var freeProxyReference = GetNextReference();
            if (freeProxyReference == null)
            {
                while (true)
                {
                    _logger.LogDebug($"No free proxy found. Total proxies count: {_proxyStates.Count} | Waiting...");

                    _proxyAvailableResetEvent.Wait(cancellation);
                    freeProxyReference = GetNextReference();

                    if (freeProxyReference != null)
                    {
                        _logger.LogDebug($"Found free proxy with id '{freeProxyReference.Proxy.Id}'");
                        break;
                    }
                }
            }

            using (RecordLocking.AcquireLock(freeProxyReference.Proxy.Id))
            {
                if (freeProxyReference.IsRented)
                {
                    _logger.LogDebug(
                        $"Unable to return proxy '{freeProxyReference.Proxy.Id}'. " +
                        $"It's already acquired by '{freeProxyReference.Proxy.RenterId}'.");
                    
                    return null;
                }

                if (_healthCheckers.Length > 0 && !freeProxyReference.LastHealthCheck.HasValue)
                {       
                    _logger.LogDebug($"Starting health check " +
                                     $"of proxy '{freeProxyReference.Proxy.Id}' with '{_healthCheckers.Length}' checkers");
                    
                    try
                    {
                        foreach (var checker in _healthCheckers)
                        {
                            if (!checker.IsAlive(freeProxyReference.Proxy))
                            {
                                _logger.LogDebug($"Health check with '{checker.GetType().Name}' " +
                                                 $"not passed on proxy '{freeProxyReference.Proxy.Id}'");

                                if (_proxyStates.Remove(freeProxyReference))
                                    _logger.LogDebug($"Proxy removed from collection. Total proxies count: {_proxyStates.Count}");
                                
                                return null;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogDebug(e, $"Health check failed on proxy '{freeProxyReference.Proxy.Id}' ");
                        
                        if (_proxyStates.Remove(freeProxyReference))
                            _logger.LogDebug($"Proxy removed from collection. Total proxies count: {_proxyStates.Count}");

                        return null;
                    }
                }
                
                freeProxyReference.Proxy.RenterId = Guid.NewGuid();
                freeProxyReference.IsRented = true;
                
                _logger.LogDebug($"Assigning proxy '{freeProxyReference.Proxy.Id}' to renter '{freeProxyReference.Proxy.RenterId}'");
                
                return freeProxyReference.Proxy;
            }
        }

        /// <inheritdoc />
        public void Return(IRentedHttpProxy proxy)
        {
            var matchedReference = _proxyStates.FirstOrDefault(p => p.Proxy.Id == proxy.Id);
            if (matchedReference == null)
            {
                // Nothing to return
                return;
            }

            using (RecordLocking.AcquireLock(matchedReference.Proxy.Id))
            {
                matchedReference.IsRented = false;
                
                _logger.LogDebug($"Proxy '{matchedReference.Proxy.Id}' " +
                                 $"returned to pool from '{matchedReference.Proxy.RenterId}'");
                
                if (!_proxyAvailableResetEvent.IsSet)
                {
                    _proxyAvailableResetEvent.Set();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpProxyHealthCheckTimer?.Dispose();
            _refreshTimer?.Dispose();
        }
    }
}