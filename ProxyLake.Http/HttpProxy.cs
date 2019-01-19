using System;
using System.Net;
using ProxyLake.Http.Abstractions;

namespace ProxyLake.Http
{
    internal class HttpProxy : WebProxy, IHttpProxy
    {
        /// <inheritdoc />
        public Guid Id { get; }

        public HttpProxy()
        {
            Id = Guid.NewGuid();
        }
    }
}