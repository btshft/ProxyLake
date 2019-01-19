using System;

namespace ProxyLake.Http
{
    internal class TrackingProxy
    {
        public RentedProxy Proxy { get;  }
        public DateTime? LastHealthCheck { get; set; }
        public bool IsRented { get; set; }
        
        public TrackingProxy(RentedProxy proxy)
        {
            Proxy = proxy;
        }
    }
}