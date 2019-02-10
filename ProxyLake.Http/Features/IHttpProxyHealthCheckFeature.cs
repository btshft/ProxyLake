using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http.Features
{
    /// <summary>
    /// Proxy health check feature.
    /// </summary>
    public interface IHttpProxyHealthCheckFeature : IHttpProxyFeature
    {
        /// <summary>
        /// Performs proxy health check.
        /// </summary>
        /// <param name="proxy">Proxy instance.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>True if proxy passed health check.</returns>
        Task<bool> IsAliveAsync(IHttpProxy proxy, CancellationToken cancellation);
    }
}