using System;
using System.Collections.Generic;

namespace ProxyLake.Http.Features
{    
    public interface IHttpProxyRotationFeature : IHttpProxyFeature
    {
        bool TryRotate(IHttpProxy currentProxy, IReadOnlyCollection<IHttpProxyState> aliveProxies, out Guid newProxyId);
    }
}