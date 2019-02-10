using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http
{
    /// <summary>
    /// Core component - creates instances of http proxy client.
    /// </summary>
    public interface IHttpProxyClientFactory
    {
        /// <summary>
        /// Returns new instance of http proxy client.
        /// </summary>
        /// <param name="name">Client name.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>Proxy client instance.</returns>
        Task<HttpProxyClient> CreateClientAsync(string name, CancellationToken cancellation);
    }
}