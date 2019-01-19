using System.Threading;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyPool
    {
        IRentedHttpProxy Rent(CancellationToken cancellation);
        IRentedHttpProxy RentNext(IHttpProxy current, CancellationToken cancellation);
        void Return(IRentedHttpProxy proxy);
    }
}