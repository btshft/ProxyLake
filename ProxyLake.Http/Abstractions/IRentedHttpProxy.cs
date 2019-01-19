using System;

namespace ProxyLake.Http.Abstractions
{
    public interface IRentedHttpProxy : IHttpProxy
    {
        Guid RenterId { get; }
        
        void Return();
    }
}