using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http.Features
{
    internal class NullHttpProxyHealthCheck : IHttpProxyHealthCheckFeature
    {
        public Task<bool> IsAliveAsync(IHttpProxy proxy, CancellationToken cancellation)
        {
            return Task.FromResult(true);
        }
    }
}