using System;
using System.Collections.Generic;

namespace ProxyLake.Http
{
    public interface IHttpProxyRotationService
    {
        bool TryRotate(IHttpProxy currentProxy, IReadOnlyCollection<IHttpProxyState> aliveProxies, out Guid newProxyId);
    }
}