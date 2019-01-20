using System;
using System.Net;

namespace ProxyLake.Http
{
    internal class HttpProxy : WebProxy, IHttpProxy
    {
        public Guid Id { get; } = Guid.NewGuid();
    }
}