using System;
using System.Net;

namespace ProxyLake.Http
{
    /// <summary>
    /// Describes http proxy.
    /// </summary>
    public interface IHttpProxy : IWebProxy
    {
        /// <summary>
        /// Unique proxy id.
        /// </summary>
        Guid Id { get; }
        
        /// <summary>
        /// Proxy address (includes port).
        /// </summary>
        Uri Address { get; }
    }
}