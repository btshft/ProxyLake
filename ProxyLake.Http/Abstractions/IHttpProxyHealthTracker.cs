using System.Collections.Generic;

namespace ProxyLake.Http.Abstractions
{
    internal interface IHttpProxyHealthTracker
    {
        void ProcessBatch(IList<TrackingProxy> proxies);
    }
}