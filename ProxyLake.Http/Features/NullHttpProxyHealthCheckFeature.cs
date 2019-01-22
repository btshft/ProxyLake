using System.Threading;

namespace ProxyLake.Http.Features
{
    internal class NullHttpProxyHealthCheckFeature : IHttpProxyHealthCheckFeature
    {
        public bool IsAlive(IHttpProxy proxy, CancellationToken cancellation)
        {
            return true;
        }
    }
}