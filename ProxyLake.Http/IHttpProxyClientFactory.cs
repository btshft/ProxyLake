using System.Net.Http;
using System.Threading;

namespace ProxyLake.Http
{
    public interface IHttpProxyClientFactory
    {
        HttpClient CreateClient(string name, CancellationToken cancellation);
    }
}