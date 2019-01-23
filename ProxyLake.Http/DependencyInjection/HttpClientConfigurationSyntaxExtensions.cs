using System;
using Microsoft.Extensions.DependencyInjection;
using ProxyLake.Http.Features;

namespace ProxyLake.Http.DependencyInjection
{
    public static class HttpClientConfigurationSyntaxExtensions
    {
        public static IHttpClientConfigurationSyntax UseHealthCheck<THealthCheckFeature>(
            this IHttpClientConfigurationSyntax syntax, TimeSpan period)
            where THealthCheckFeature : class, IHttpProxyHealthCheckFeature
        {
            syntax.Services.Configure<HttpProxyClientFactoryOptions>(
                syntax.Name, o => o.HealthCheckPeriod = period);

            syntax.Services.AddFeature<IHttpProxyHealthCheckFeature, THealthCheckFeature>(syntax.Name);
            
            return syntax;
        }

        public static IHttpClientConfigurationSyntax UseRefill<TProxyRefillFeature>(
            this IHttpClientConfigurationSyntax syntax, TimeSpan period, 
            int minProxyCountInclusive, int maxProxyCountInclusive)
            where TProxyRefillFeature : class, IHttpProxyRefill
        {
            syntax.Services.Configure<HttpProxyClientFactoryOptions>(
                syntax.Name, o =>
                {
                    o.ProxyRefillPeriod = period;
                    o.MaxProxyCount = maxProxyCountInclusive;
                    o.MinProxyCount = minProxyCountInclusive;
                });

            syntax.Services.AddFeature<IHttpProxyRefill, TProxyRefillFeature>(syntax.Name);
            
            return syntax;
        }
    }
}