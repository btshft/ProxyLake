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
    internal class DefaultHttpProxyClientFactory : IHttpProxyClientFactory, IDisposable
    {
        private static readonly TimeSpan DefaultCleanupPeriod = TimeSpan.FromSeconds(15);

        private readonly IHttpProxyDefinitionFactory _definitionFactory;
        private readonly IHttpProxyFactory _httpProxyFactory;
        private readonly IHttpProxyLoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<HttpProxyClientFactoryOptions> _optionsMonitor;

        private readonly IHttpProxyFeatureProvider<IHttpProxyRotationFeature> _rotationFeatureProvider;
        private readonly IHttpProxyFeatureProvider<IHttpProxyHealthCheckFeature> _healthCheckFeatureProvider;
        private readonly IHttpProxyFeatureProvider<IHttpProxyRefill> _proxyRefillFeatureProvider;

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
            IHttpProxyFeatureProvider<IHttpProxyRefill> proxyRefillFeatureProvider,
            IOptionsMonitor<HttpProxyClientFactoryOptions> optionsMonitor)
        {
            _loggerFactory = loggerFactory;
            _rotationFeatureProvider = rotationFeatureProvider;
            _healthCheckFeatureProvider = healthCheckFeatureProvider;
            _optionsMonitor = optionsMonitor;
            _proxyRefillFeatureProvider = proxyRefillFeatureProvider;
            _definitionFactory = definitionFactory;
            _httpProxyFactory = httpProxyFactory;
            _logger = loggerFactory.CreateLogger(typeof(DefaultHttpProxyClientFactory));
            _clientStates = new ConcurrentDictionary<string, Lazy<HttpProxyClientState>>(
                StringComparer.InvariantCultureIgnoreCase);

            _lastAcquiredProxies =
                new ConcurrentDictionary<string, IHttpProxy>(StringComparer.InvariantCultureIgnoreCase);

            _deadHandlers = new ConcurrentQueue<DeadHandlerReference>();
            _cleanupActivity = new HandlersCleanupActivity(DefaultCleanupPeriod, _deadHandlers, loggerFactory);
        }

        /// <inheritdoc />
        public HttpProxyClient CreateClient(string name, CancellationToken cancellation)
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
            var spin = new SpinWait();
            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                spin.SpinOnce();

                var handlerStates = GetProxyClientState().HandlerStates;
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
                        var handler = TryAcquireHandlerState(prevProxyId, proxyId, handlerStates);
                        if (handler != null)
                            return handler;
                    }
                }
                else
                {
                    _logger.LogDebug($"Proxy rotation failed. Last acquired: '{prevProxyId}'");
                }
            }

            HttpProxyClientState GetProxyClientState()
            {
                var clientState = _clientStates.GetOrAdd(name, LazyProxyClientState).Value;

                if (!clientState.HealthCheckActivity.IsRunning)
                    clientState.HealthCheckActivity.Start(cancellation);

                if (!clientState.RefillActivity.IsRunning)
                    clientState.RefillActivity.Start(cancellation);

                return clientState;
            }

            HttpProxyHandlerState TryAcquireHandlerState(
                Guid? prevProxyId, Guid newProxyId, IEnumerable<HttpProxyHandlerState> handlerStates)
            {
                var rotatedHandler = handlerStates.FirstOrDefault(s => s.ProxyState.Proxy.Id == newProxyId);
                if (rotatedHandler != null && rotatedHandler.ProxyState.TryAcquireProxy())
                {
                    _lastAcquiredProxies.AddOrUpdate(
                        key: name,
                        addValueFactory: _ =>
                        {
                            _logger.LogDebug($"Acquired first proxy '{rotatedHandler.ProxyState.Proxy.Id}'");
                            return rotatedHandler.ProxyState.Proxy;
                        },
                        updateValueFactory: (_, prev) =>
                        {
                            _logger.LogDebug(
                                $"Proxy rotated '{prev.Id}' -> '{rotatedHandler.ProxyState.Proxy.Id}'");
                            prevProxyId = prev.Id;
                            return rotatedHandler.ProxyState.Proxy;
                        });

                    return rotatedHandler;
                }

                #if DEBUG
                
                if (prevProxyId.HasValue)
                {
                    _logger.LogDebug(rotatedHandler == null
                        ? $"Unable to switch proxy '{prevProxyId}' -> '{newProxyId}' - new proxy was collected by GC"
                        : $"Unable to switch proxy '{prevProxyId}' -> '{newProxyId}' - new proxy was lost in data-race");
                }
                else
                {
                    _logger.LogDebug(rotatedHandler == null
                        ? $"Unable to acquire proxy '{newProxyId}' - GC already collect it"
                        : $"Unable to acquire proxy '{newProxyId}' - it's lost in data-race ");
                }
                
                #endif

                return null;
            }

            Lazy<HttpProxyClientState> LazyProxyClientState(string _)
            {
                return new Lazy<HttpProxyClientState>(
                    () => CreateProxyClientState(name, cancellation),
                    LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }

        private HttpProxyClientState CreateProxyClientState(string name, CancellationToken cancellation)
        {
            var handlerStates = CreateProxyHandlerStates(name, cancellation)
                .AsCopyOnWriteCollection();

            var healthCheckFeature = _healthCheckFeatureProvider.GetFeature(name);
            var refillProxyFeature = _proxyRefillFeatureProvider.GetFeature(name);

            var options = _optionsMonitor.Get(name);
            options.EnsureValid();

            var healthCheckActivity = healthCheckFeature is NullHttpProxyHealthCheck
                ? (IScheduledActivity) new NullScheduledActivity()
                : new ProxyHealthCheckActivity(
                    options.HealthCheckPeriod, _loggerFactory, healthCheckFeature,
                    s => RemoveProxyHandlerState(name, s),
                    () => GetProxyHandlerStates(name),
                    InsertDeadHandler);

            var refillActivity = refillProxyFeature is NullHttpProxyRefill
                ? (IScheduledActivity) new NullScheduledActivity()
                : new ProxyRefillActivity(
                    options.ProxyRefillPeriod, options, _loggerFactory,
                    () => GetAliveProxies(name),
                    p => InsertProxy(name, p),
                    refillProxyFeature, 
                    _httpProxyFactory);

            return new HttpProxyClientState(handlerStates, healthCheckActivity, refillActivity);
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

                if (handlerStates.Any(h => h.ProxyState.Proxy.Address == proxy.Address))
                {
                    _logger.LogInformation($"Skipping proxy '{proxy.Address}'. It's already exists in collection");
                    continue;
                }
                
                handlerStates.Add(new HttpProxyHandlerState(new HttpProxyState(proxy)));

                _logger.LogDebug($"Added proxy '{proxy.Id}' with URL '{proxy.Address}'");
            }

            return handlerStates;
        }

        private IReadOnlyCollection<IHttpProxy> GetAliveProxies(string name)
        {
            if (_clientStates.TryGetValue(name, out var state))
            {
                return state.Value.HandlerStates
                    .Select(s => s.ProxyState.Proxy).ToArray();
            }

            return Array.Empty<IHttpProxy>();
        }

        private void InsertProxy(string name, IHttpProxy proxy)
        {
            if (proxy == null)
                throw new ArgumentNullException(nameof(proxy));

            if (_clientStates.TryGetValue(name, out var state))
            {
                if (state.Value.HandlerStates.Any(s => s.ProxyState.Proxy.Address == proxy.Address))
                    throw new Exception($"Proxy with address '{proxy.Address}' already exists");
                
                state.Value.HandlerStates.Add(
                    new HttpProxyHandlerState(new HttpProxyState(proxy)));
            }
        }

        private bool RemoveProxyHandlerState(string name, HttpProxyHandlerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            return _clientStates.TryGetValue(name, out var clientState) &&
                   clientState.Value.HandlerStates.Remove(state);
        }

        private void InsertDeadHandler(DeadHandlerReference reference)
        {
            _deadHandlers.Enqueue(reference);
        }

        private IReadOnlyCollection<HttpProxyHandlerState> GetProxyHandlerStates(string name)
        {
            return _clientStates.TryGetValue(name, out var state)
                ? state.Value.HandlerStates.ToArray()
                : Array.Empty<HttpProxyHandlerState>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _loggerFactory?.Dispose();
            _cleanupActivity?.Dispose();

            foreach (var state in _clientStates.Values)
            {
                state.Value.RefillActivity?.Dispose();
                state.Value.HealthCheckActivity?.Dispose();
            }
        }
    }
}