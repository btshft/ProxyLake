using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Logging;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http
{
    
    // TODO: Background health check, on-create health check, background refill
    internal class DefaultHttpProxyClientFactory : IHttpProxyClientFactory
    {
        private readonly IHttpProxyDefinitionProvider _definitionProvider;
        private readonly IHttpProxyFactory _httpProxyFactory;
        private readonly IHttpProxyLoggerFactory _loggerFactory;
        private readonly IHttpProxyRotationService _rotationService;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, Lazy<IReadOnlyCollection<HttpProxyHandlerState>>> _handlerStates;
        private readonly ConcurrentDictionary<string, IHttpProxy> _lastAcquiredProxies;

        public DefaultHttpProxyClientFactory(
            IHttpProxyDefinitionProvider definitionProvider,
            IHttpProxyFactory httpProxyFactory,
            IHttpProxyLoggerFactory loggerFactory,
            IHttpProxyRotationService rotationService)
        {
            _loggerFactory = loggerFactory;
            _rotationService = rotationService;
            _definitionProvider = definitionProvider;
            _httpProxyFactory = httpProxyFactory;
            _logger = loggerFactory.CreateLogger(typeof(DefaultHttpProxyClientFactory));
            _handlerStates = new ConcurrentDictionary<string, Lazy<IReadOnlyCollection<HttpProxyHandlerState>>>(
                StringComparer.InvariantCultureIgnoreCase);
            
            _lastAcquiredProxies = new ConcurrentDictionary<string, IHttpProxy>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <inheritdoc />
        public HttpClient CreateClient(string name, CancellationToken cancellation)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            var handlerState = AcquireHandlerState(name, cancellation);
            return new HttpProxyClient(handlerState, _loggerFactory);;
        }

        private HttpProxyHandlerState AcquireHandlerState(string name, CancellationToken cancellation)
        {
            using (SpinWaitBarrier.Create(spinWaitMultiplier: 500))
            {
                while (true)
                {
                    cancellation.ThrowIfCancellationRequested();

                    var handlerStates = _handlerStates.GetOrAdd(name, CreateLazyHandlerStates).Value;
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

                    if (_rotationService.TryRotate(lastAcquiredProxy, proxyStates, out var proxyId))
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

                    Thread.SpinWait(iterations: 100);
                }
            }

            Lazy<IReadOnlyCollection<HttpProxyHandlerState>> CreateLazyHandlerStates(string _)
            {
                return new Lazy<IReadOnlyCollection<HttpProxyHandlerState>>(
                    () => CreateProxyHandlerStates(name), LazyThreadSafetyMode.ExecutionAndPublication);
            }
        }
    
        private IReadOnlyCollection<HttpProxyHandlerState> CreateProxyHandlerStates(string name)
        {
            var handlerStates = new List<HttpProxyHandlerState>();

            var proxyDefinitions = _definitionProvider.GetProxyDefinitions(name).ToArray();
            if (proxyDefinitions.Length == 0)
                throw new Exception("Proxy definition provider returned empty result");

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