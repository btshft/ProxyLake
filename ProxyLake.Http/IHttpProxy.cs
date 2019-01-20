using System;
using System.Net;

namespace ProxyLake.Http
{
    public interface IHttpProxy : IWebProxy
    {
        Guid Id { get; }
        Uri Address { get; }
    }
}