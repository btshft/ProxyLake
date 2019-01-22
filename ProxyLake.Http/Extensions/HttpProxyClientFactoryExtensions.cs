using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Options;

namespace ProxyLake.Http.Extensions
{
    public static class HttpProxyClientFactoryExtensions
    {
        public static HttpClient CreateDefaultClient(
            this IHttpProxyClientFactory factory, CancellationToken cancellation = default)
        {
            return factory.CreateClient(Options.DefaultName, cancellation);
        }
    }
}