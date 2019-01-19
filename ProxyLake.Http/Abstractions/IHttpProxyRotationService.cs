using System.Threading;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyRotationService
    {
        bool IsRotationRequired(IHttpProxy proxy);
        bool TryRotate(IHttpProxy current, CancellationToken cancellationToken, out IRentedHttpProxy newProxy);
    }
}