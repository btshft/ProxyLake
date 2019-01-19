using System;
using System.Net;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http
{
    internal class DefaultProxyFactory : IHttpProxyFactory
    {
        private readonly ILogger _logger;

        public DefaultProxyFactory(IHttpProxyLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(DefaultProxyFactory));
        }
        
        /// <inheritdoc />
        public IHttpProxy CreateProxy(HttpProxyDefinition definition)
        {
            var fullUri = definition.Port.HasValue
                ? $"{definition.Host}:{definition.Port}"
                : $"{definition.Host}";

            if (!Uri.TryCreate(fullUri, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, "http", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception($"Proxy URL '{fullUri}' is not valid. Expected http only absolute URI");
            }
            
            var proxy = new HttpProxy
            {
                Address = uri,
                BypassProxyOnLocal = definition.BypassOnLocal,
                Credentials = definition.Username != null 
                    ? new NetworkCredential(definition.Username, definition.Password) 
                    : null
            };

            _logger.LogDebug($"Created new proxy with id '{proxy.Id}' from URL '{uri}'.");
            
            return proxy;
        }
    }
}