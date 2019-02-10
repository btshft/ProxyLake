using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http.Features
{
    /// <summary>
    /// Proxy refill feature.
    /// </summary>
    public interface IHttpProxyRefill : IHttpProxyFeature
    {
        /// <summary>
        /// Returns a set of new proxies.
        /// </summary>
        /// <param name="aliveProxies">Alive proxies.</param>
        /// <param name="maxProxiesCount">Requested proxies count.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>A set of new proxies.</returns>
        Task<IReadOnlyCollection<HttpProxyDefinition>> GetProxiesAsync(
            IReadOnlyCollection<IHttpProxy> aliveProxies, int maxProxiesCount, CancellationToken cancellation);
    }
}