using System.Collections.Generic;
using ProxyLake.Http.Abstractions;

namespace ProxyLake.Http
{
    internal class NullProxyRefreshService : IHttpProxyRefreshService
    {
        /// <inheritdoc />
        public IEnumerable<IHttpProxy> RefreshProxies(IEnumerable<IHttpProxy> aliveProxies, int? maxProxiesCount)
        {
            return aliveProxies;
        }
    }
}