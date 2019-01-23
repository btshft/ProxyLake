using System.Threading;

namespace ProxyLake.Http.Features
{
    internal class NullHttpProxyHealthCheck : IHttpProxyHealthCheckFeature
    {
        public bool IsAlive(IHttpProxy proxy, CancellationToken cancellation)
        {
            return true;
        }
    }
}