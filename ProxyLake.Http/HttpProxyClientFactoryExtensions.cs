using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Options;

namespace ProxyLake.Http
{
    public static class HttpProxyClientFactoryExtensions
    {
        public static HttpClient CreateClient(this IHttpProxyClientFactory factory, string name)
        {
            return factory.CreateClient(name, CancellationToken.None);
        }

        public static HttpClient CreateClient(this IHttpProxyClientFactory factory)
        {
            return factory.CreateClient(Options.DefaultName, CancellationToken.None);
        }

        public static HttpClient CreateClient(this IHttpProxyClientFactory factory, CancellationToken cancellation)
        {
            return factory.CreateClient(Options.DefaultName, cancellation);
        }
    }
}