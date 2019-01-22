using System.Threading;

namespace ProxyLake.Http.Features
{
    public interface IHttpProxyHealthCheckFeature : IHttpProxyFeature
    {
        bool IsAlive(IHttpProxy proxy, CancellationToken cancellation);
    }
}