using System.Collections.Generic;
using System.Threading;

namespace ProxyLake.Http.Features
{
    public interface IHttpProxyRefill : IHttpProxyFeature
    {
        IReadOnlyCollection<IHttpProxy> GetProxies(
            IReadOnlyCollection<IHttpProxy> aliveProxies, int maxProxiesCount, CancellationToken cancellation);
    }
}