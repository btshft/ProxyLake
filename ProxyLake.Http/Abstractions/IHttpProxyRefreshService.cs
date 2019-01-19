using System.Collections.Generic;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyRefreshService
    {
        IEnumerable<IHttpProxy> RefreshProxies(IEnumerable<IHttpProxy> aliveProxies, int? maxProxiesCount);
    }
}