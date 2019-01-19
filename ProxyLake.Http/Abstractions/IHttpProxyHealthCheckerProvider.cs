using System.Collections.Generic;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyHealthCheckerProvider
    {
        IReadOnlyCollection<IHttpProxyHealthChecker> GetHealthCheckers();
    }
}