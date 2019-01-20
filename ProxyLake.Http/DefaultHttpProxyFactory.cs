using System;
using System.Net;

namespace ProxyLake.Http
{
    internal class DefaultHttpProxyFactory : IHttpProxyFactory
    {
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

            return proxy;
        }
    }
}