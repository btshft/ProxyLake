using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http.Features
{
    internal class NullHttpProxyRefill : IHttpProxyRefill
    {
        public Task<IReadOnlyCollection<HttpProxyDefinition>> GetProxiesAsync(
            IReadOnlyCollection<IHttpProxy> aliveProxies, int maxProxiesCount, CancellationToken cancellation)
        {
            return Task.FromResult<IReadOnlyCollection<HttpProxyDefinition>>(
                Array.Empty<HttpProxyDefinition>());
        }
    }
}