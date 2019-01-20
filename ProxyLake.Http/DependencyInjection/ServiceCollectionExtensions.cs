using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpProxyClient<TProxyProvider>(this IServiceCollection services)
            where TProxyProvider : class, IHttpProxyDefinitionProvider
        {
            services.AddLogging();
            services.AddOptions();

            services.TryAddSingleton<IHttpProxyClientFactory, DefaultHttpProxyClientFactory>();
            services.TryAddSingleton<IHttpProxyDefinitionProvider, TProxyProvider>();
            services.TryAddSingleton<IHttpProxyFactory, DefaultHttpProxyFactory>();
            services.TryAddSingleton<IHttpProxyLoggerFactory, HttpProxyLoggerFactory>();
            services.TryAddSingleton<IHttpProxyRotationService, RoundRobinHttpProxyRotationService>();
            
            return services;
        }
    }
}