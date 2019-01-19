using System;

namespace ProxyLake.Http
{
    public class HttpProxyClientFactoryOptions
    {
        public TimeSpan HealthCheckDueTime { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan HealthCheckPeriod { get; set; } = TimeSpan.FromSeconds(30);
        
        public TimeSpan ProxyRefreshDueTime { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan ProxyRefreshPeriod { get; set; } = TimeSpan.FromSeconds(60);
        
        public int AliveProxiesLowerLimit { get; set; } = 3;
        public int AliveProxiesUpperLimit { get; set; } = 10;
    }
}