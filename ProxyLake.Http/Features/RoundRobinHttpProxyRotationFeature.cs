using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxyLake.Http.Features
{
    internal class RoundRobinHttpProxyRotationFeature : IHttpProxyRotationFeature
    {
        /// <inheritdoc />
        public bool TryRotate(IHttpProxy currentProxy, IReadOnlyCollection<IHttpProxyState> aliveProxies,
            out Guid newProxyId)
        {
            if (currentProxy != null)
            {
                var position = Array.IndexOf(aliveProxies.Select(a => a.Proxy).ToArray(), currentProxy);
                if (position != -1 && position + 1 != aliveProxies.Count)
                {
                    var nextFreeProxy = aliveProxies
                        .Skip(position + 1)
                        .FirstOrDefault(a => !a.IsProxyAcquired);

                    if (nextFreeProxy != null)
                    {
                        newProxyId = nextFreeProxy.Proxy.Id;
                        return true;
                    }
                }
            }

            var freeProxy = aliveProxies.FirstOrDefault(s => !s.IsProxyAcquired);
            if (freeProxy != null)
                newProxyId = freeProxy.Proxy.Id;

            return freeProxy != null;
        }
    }
}