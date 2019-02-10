using System.Net.Http;
using System.Threading;

namespace ProxyLake.Http
{
    public interface IHttpProxyClientFactory
    {
        HttpProxyClient CreateClient(string name, CancellationToken cancellation);
    }
}