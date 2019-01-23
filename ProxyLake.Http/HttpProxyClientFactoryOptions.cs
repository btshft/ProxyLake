using System;

namespace ProxyLake.Http
{
    public class HttpProxyClientFactoryOptions
    {
        public TimeSpan HealthCheckPeriod { get; set; } = TimeSpan.FromSeconds(30);
        
        public TimeSpan ProxyRefillPeriod { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Inclusive.
        /// </summary>
        public int MinProxyCount { get; set; } = 5;

        /// <summary>
        /// Inclusive.
        /// </summary>
        public int MaxProxyCount { get; set; } = 50;

        internal void EnsureValid()
        {
            if (HealthCheckPeriod < TimeSpan.Zero)
                throw new Exception($"Invalid {nameof(HealthCheckPeriod)} parameter. " +
                                    $"Should be >= {nameof(TimeSpan)}.{nameof(TimeSpan.Zero)}");
            
            if (ProxyRefillPeriod < TimeSpan.Zero) 
                throw new Exception($"Invalid {nameof(ProxyRefillPeriod)} parameter. " +
                                    $"Should be >= {nameof(TimeSpan)}.{nameof(TimeSpan.Zero)}");
            
            if (MinProxyCount < 0)
                throw new Exception($"Invalid {nameof(MinProxyCount)} parameter. Should be > 0");
            
            if (MaxProxyCount < 0 || MaxProxyCount < MinProxyCount)
                throw new Exception($"Invalid {nameof(MaxProxyCount)} parameter. Should be > 0 && >= {nameof(MinProxyCount)}");
            
        }
    }
}