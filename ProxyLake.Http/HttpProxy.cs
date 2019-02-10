using System;
using System.Net;

namespace ProxyLake.Http
{
    internal class HttpProxy : WebProxy, IHttpProxy
    {
        public Guid Id { get; }

        internal HttpProxy()
        {
            Id = Guid.NewGuid();
        }

        internal HttpProxy(IHttpProxy source)
        {
            Id = source.Id;
            Address = source.Address;
            Credentials = source.Credentials;
        }
    }
}