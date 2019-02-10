using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ProxyLake.Http.Extensions
{
    /// <summary>
    /// A set of extensions for <see cref="IHttpProxyClientFactory"/>.
    /// </summary>
    public static class HttpProxyClientFactoryExtensions
    {
        /// <summary>
        /// Creates default http proxy client.
        /// </summary>
        public static Task<HttpProxyClient> CreateDefaultClientAsync(
            this IHttpProxyClientFactory factory, CancellationToken cancellation = default)
        {
            return factory.CreateClientAsync(Options.DefaultName, cancellation);
        }
    }
}