using System;
using System.Collections.Generic;
using System.Threading;

namespace ProxyLake.Http.Features
{
    internal class NullHttpProxyRefill : IHttpProxyRefill
    {
        public IReadOnlyCollection<IHttpProxy> GetProxies(
            IReadOnlyCollection<IHttpProxy> aliveProxies, int maxProxiesCount, CancellationToken cancellation)
        {
            return Array.Empty<IHttpProxy>();
        }
    }
}