using System.Threading;

namespace ProxyLake.Http.Abstractions
{
    public interface IRotationSupportHttpProxy : IHttpProxy
    {
        bool TryRotate(CancellationToken cancellationToken);
    }
}