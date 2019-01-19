using System;
using System.Net.Http;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http
{
    internal class DefaultProxyClientFactory : IHttpProxyClientFactory, IDisposable
    {
        private readonly IHttpProxyPool _httpProxyPool;
        private readonly IHttpProxyRotationService _rotationService;
        private readonly IHttpProxyLoggerFactory _loggerFactory;
        
        private readonly Lazy<ProxyClientHandler> _cachedHandlerProvider;

        public DefaultProxyClientFactory(
            IHttpProxyPool httpProxyPool, 
            IHttpProxyRotationService rotationService, 
            IHttpProxyLoggerFactory loggerFactory)
        {
            _httpProxyPool = httpProxyPool;
            _rotationService = rotationService;
            _loggerFactory = loggerFactory;
            _cachedHandlerProvider = new Lazy<ProxyClientHandler>(CreateHandler);
        }
        
        /// <inheritdoc />
        public HttpClient CreateProxyClient()
        {
            return new HttpClient(_cachedHandlerProvider.Value, disposeHandler: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_cachedHandlerProvider.IsValueCreated)
                _cachedHandlerProvider.Value?.Dispose();
        }

        /// <summary>
        /// Creates a new instance of <see cref="ProxyClientHandler"/>.
        /// </summary>
        /// <returns>Instance of <see cref="ProxyClientHandler"/>.</returns>
        private ProxyClientHandler CreateHandler()
        {
            return new ProxyClientHandler(_loggerFactory,
                new RotationSupportHttpProxy(_rotationService, _httpProxyPool));
        }
    }
}