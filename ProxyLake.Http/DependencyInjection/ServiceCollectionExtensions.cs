using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProxyLake.Http.Abstractions;
using ProxyLake.Http.BackgroundActivity;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpProxyClient<TProxyProvider, THealthCheckerProvider, TRefreshService>(
            this IServiceCollection services, Action<HttpProxyClientFactoryOptions> configurator) 
            where TProxyProvider : class, IHttpProxyDefinitionProvider
            where THealthCheckerProvider : class, IHttpProxyHealthCheckerProvider
            where TRefreshService : class, IHttpProxyRefreshService
        {
            services.AddLogging();
            services.AddOptions();

            services.Configure<HttpProxyClientFactoryOptions>(configurator);
            
            services.TryAddSingleton<IHttpProxyClientFactory, DefaultProxyClientFactory>();
            services.TryAddSingleton<IHttpProxyDefinitionProvider, TProxyProvider>();
            services.TryAddSingleton<IHttpProxyPool, DefaultProxyPool>();
            services.TryAddSingleton<IHttpProxyRotationService, RoundRobinProxyRotationService>();
            services.TryAddSingleton<IHttpProxyHealthCheckerProvider, THealthCheckerProvider>();
            services.TryAddSingleton<IHttpProxyHealthTracker, DefaultProxyHealthTracker>();
            services.TryAddSingleton<IHttpProxyFactory, DefaultProxyFactory>();
            services.TryAddSingleton<IHttpProxyLoggerFactory, ProxyLoggerFactory>();
            services.TryAddSingleton<IHttpProxyRefreshService, TRefreshService>();
            
            return services;
        }
        
        public static IServiceCollection AddHttpProxyClient<TProxyProvider, THealthCheckerProvider, TRefreshService>(
            this IServiceCollection services) 
            where TProxyProvider : class, IHttpProxyDefinitionProvider
            where THealthCheckerProvider : class, IHttpProxyHealthCheckerProvider
            where TRefreshService : class, IHttpProxyRefreshService
        {
            return services.AddHttpProxyClient<TProxyProvider, THealthCheckerProvider, TRefreshService>(_ => { });
        }
        
        public static IServiceCollection AddHttpProxyClient<TProxyProvider>(
            this IServiceCollection services, Action<HttpProxyClientFactoryOptions> configurator) 
            where TProxyProvider : class, IHttpProxyDefinitionProvider
        {
            return services.AddHttpProxyClient<TProxyProvider, NullProxyHealthCheckerProvider, NullProxyRefreshService>(configurator);
        }
        
        public static IServiceCollection AddHttpProxyClient<TProxyProvider>(
            this IServiceCollection services) 
            where TProxyProvider : class, IHttpProxyDefinitionProvider
        {
            return services.AddHttpProxyClient<TProxyProvider, NullProxyHealthCheckerProvider, NullProxyRefreshService>(_ => { });
        }
    }
}