using System;
using System.Collections.Generic;
using ProxyLake.Http.Abstractions;

namespace ProxyLake.Http
{
    internal class NullProxyHealthCheckerProvider : IHttpProxyHealthCheckerProvider
    {
        public IReadOnlyCollection<IHttpProxyHealthChecker> GetHealthCheckers()
        {
            return Array.Empty<IHttpProxyHealthChecker>();
        }
    }
}