using System.Net.Http;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyClientFactory
    {
        HttpClient CreateProxyClient();
    }
}