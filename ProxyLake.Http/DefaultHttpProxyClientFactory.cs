using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyLake.Http.Extensions;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;
using ProxyLake.Http.ScheduledActivities;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http
{
    // TODO: on-create health check (maybe configurable?), background proxy refill, proxy wait logic, parallel health check (?)
    internal class DefaultHttpProxyClientFactory : IHttpProxyClientFactory
    {
        private static readonly TimeSpan DefaultCleanupPeriod = TimeSpan.FromSeconds(15);
        
        private readonly IHttpProxyDefinitionFactory _definitionFactory;
        private readonly IHttpProxyFactory _httpProxyFactory;
        private readonly IHttpProxyLoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<HttpProxyClientFactoryOptions> _optionsMonitor;

        private readonly IHttpProxyFeatureProvider<IHttpProxyRotationFeature> _rotationFeatureProvider;
        private readonly IHttpProxyFeatureProvider<IHttpProxyHealthCheckFeature> _healthCheckFeatureProvider;

        private readonly ConcurrentDictionary<string, Lazy<HttpProxyClientState>> _clientStates;
        private readonly ConcurrentDictionary<string, IHttpProxy> _lastAcquiredProxies;
        private readonly ConcurrentQueue<DeadHandlerReference> _deadHandlers;

        private readonly HandlersCleanupActivity _cleanupActivity;

        public DefaultHttpProxyClientFactory(
            IHttpProxyDefinitionFactory definitionFactory,
            IHttpProxyFactory httpProxyFactory,
            IHttpProxyLoggerFactory loggerFactory,
            IHttpProxyFeatureProvider<IHttpProxyRotationFeature> rotationFeatureProvider, 
            IHttpProxyFeatureProvider<IHttpProxyHealthCheckFeature> healthCheckFeatureProvider, 
            IOptionsMonitor<HttpProxyClientFactoryOptions> optionsMonitor)
        {
            _loggerFactory = loggerFactory;
            _rotationFeatureProvider = rotationFeatureProvider;
            _healthCheckFeatureProvider = healthCheckFeatureProvider;
            _optionsMonitor = optionsMonitor;
            _definitionFactory = definitionFactory;
            _httpProxyFactory = httpProxyFactory;
            _logger = loggerFactory.CreateLogger(typeof(DefaultHttpProxyClientFactory));
            _clientStates = new ConcurrentDictionary<string, Lazy<HttpProxyClientState>>(
                StringComparer.InvariantCultureIgnoreCase);

            _lastAcquiredProxies = new ConcurrentDictionary<string, IHttpProxy>(StringComparer.InvariantCultureIgnoreCase);

            _deadHandlers = new ConcurrentQueue<DeadHandlerReference>();
            _cleanupActivity = new HandlersCleanupActivity(DefaultCleanupPeriod,  _deadHandlers, loggerFactory);
        }

        /// <inheritdoc />
        public HttpClient CreateClient(string name, CancellationToken cancellation)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!_cleanupActivity.IsRunning)
                _cleanupActivity.Start(cancellation);
            
            var handlerState = AcquireHandlerState(name, cancellation);
            return new HttpProxyClient(handlerState, _loggerFactory);
        }

        private HttpProxyHandlerState AcquireHandlerState(string name, CancellationToken cancellation)
        {
            using (SpinWaitBarrier.Create())
            {
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();

                    var clientState = _clientStates.GetOrAdd(name, CreateClientState).Value;
                    if (!clientState.HealthCheckActivity.IsRunning)
                        clientState.HealthCheckActivity.Start(cancellation);
                    
                    var handlerStates = _clientStates.GetOrAdd(name, CreateClientState).Value.HandlerStates;

                    var lastAcquiredProxy = _lastAcquiredProxies.TryGetValue(name, out var acquiredProxy)
                        ? acquiredProxy
                        : null;

                    var prevProxyId = lastAcquiredProxy?.Id;

                    if (handlerStates.Count == 0 ||
                        handlerStates.Count(s => !s.ProxyState.IsProxyAcquired) == 0)
                    {
                        // TODO: Implement wait logic
                        continue;
                    }

                    var proxyStates = handlerStates.Select(s => s.ProxyState).ToArray();
                    var rotationFeature = _rotationFeatureProvider.GetFeature(name);

                    if (rotationFeature.TryRotate(lastAcquiredProxy, proxyStates, out var proxyId))
                    {
                        using (RecordLocking.AcquireLock(proxyId))
                        {
                            var rotatedProxy = handlerStates.FirstOrDefault(s => s.ProxyState.Proxy.Id == proxyId);
                            if (rotatedProxy != null && rotatedProxy.ProxyState.TryAcquireProxy())
                            {
                                _lastAcquiredProxies.AddOrUpdate(
                                    key: name,
                                    addValueFactory: _ =>
                                    {
                                        _logger.LogDebug($"Acquired first proxy '{rotatedProxy.ProxyState.Proxy.Id}'");
                                        return rotatedProxy.ProxyState.Proxy;
                                    },
                                    updateValueFactory: (_, prev) =>
                                    {
                                        _logger.LogDebug(
                                            $"Proxy rotated '{prev.Id}' -> '{rotatedProxy.ProxyState.Proxy.Id}'");
                                        prevProxyId = prev.Id;
                                        return rotatedProxy.ProxyState.Proxy;
                                    });

                                return rotatedProxy;
                            }

                            if (prevProxyId.HasValue)
                            {
                                _logger.LogDebug(rotatedProxy == null
                                    ? $"Unable to rotate proxy '{prevProxyId}' -> '{proxyId}' - seems like is was GC'ed"
                                    : $"Unable to rotate proxy '{prevProxyId}' -> '{proxyId}' - it's stolen by someone else");
                            }
                            else
                            {
                                _logger.LogDebug(rotatedProxy == null
                                    ? $"Unable to acquire proxy '{proxyId}' - seems like is was GC'ed"
                                    : $"Unable to acquire proxy '{proxyId}' - it's stolen by someone else");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"Unable to acquire new proxy. Last acquired: '{prevProxyId}'");
                    }
                }
            }

            Lazy<HttpProxyClientState> CreateClientState(string _)
            {
                return new Lazy<HttpProxyClientState>(() =>
                {
                    var handlerStates = CreateProxyHandlerStates(name, cancellation)
                        .AsCopyOnWriteCollection();

                    var healthCheckFeature = _healthCheckFeatureProvider.GetFeature(name);
                    var options = _optionsMonitor.Get(name);
                    
                    var healthCheckActivity = !(healthCheckFeature is NullHttpProxyHealthCheckFeature)
                        ? new ProxyHealthCheckActivity(
                            options.HealthCheckPeriod, handlerStates, _deadHandlers, _loggerFactory, healthCheckFeature)
                        : (IScheduledActivity) new NullScheduledActivity();
                    
                    return new HttpProxyClientState(handlerStates, healthCheckActivity);
                });
            }
        }

        private List<HttpProxyHandlerState> CreateProxyHandlerStates(
            string name, CancellationToken cancellation)
        {
            var handlerStates = new List<HttpProxyHandlerState>();
            var proxyDefinitions = _definitionFactory.CreateDefinitions(name, cancellation);

            if (proxyDefinitions == null || proxyDefinitions.Count == 0)
                throw new Exception("Proxy definition factory returned empty result");

            foreach (var definition in proxyDefinitions)
            {
                var proxy = _httpProxyFactory.CreateProxy(definition);

                handlerStates.Add(new HttpProxyHandlerState(new HttpProxyState(proxy)));

                _logger.LogDebug($"Added proxy '{proxy.Id}' with URL '{proxy.Address}'");
            }

            return handlerStates;
        }
    }
}