using System;
using System.Collections.Generic;

namespace ProxyLake.Http.Features
{
    /// <summary>
    /// Proxy rotations feature - describes mechanics to switch between proxies.
    /// </summary>
    public interface IHttpProxyRotationFeature : IHttpProxyFeature
    {
        /// <summary>
        /// Performs proxy rotation.
        /// </summary>
        /// <param name="currentProxy">Current active proxy.</param>
        /// <param name="aliveProxies">Alive proxies to select new one.</param>
        /// <param name="newProxyId">Selected proxy id.</param>
        /// <returns>True if proxy rotation succeed.</returns>
        bool TryRotate(IHttpProxy currentProxy, IReadOnlyCollection<IHttpProxyState> aliveProxies, out Guid newProxyId);
    }
}