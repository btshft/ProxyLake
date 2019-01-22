using System;

namespace ProxyLake.Http
{
    public class HttpProxyClientFactoryOptions
    {
        public TimeSpan HealthCheckPeriod { get; set; } = TimeSpan.FromSeconds(30);
    }
}