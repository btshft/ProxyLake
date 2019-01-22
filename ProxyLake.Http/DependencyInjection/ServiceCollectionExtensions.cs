using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultHttpProxyClient<TProxyProvider, THealthCheckFeature>(
            this IServiceCollection services, Action<HttpProxyClientFactoryOptions> optionsConfigurator)
            where TProxyProvider : class, IHttpProxyDefinitionFactory
            where THealthCheckFeature : class, IHttpProxyHealthCheckFeature
        {
            services.AddLogging();
            services.AddOptions();

            services.Configure(optionsConfigurator);
            
            services.TryAddSingleton<IHttpProxyClientFactory, DefaultHttpProxyClientFactory>();
            services.TryAddSingleton<IHttpProxyDefinitionFactory, TProxyProvider>();
            services.TryAddSingleton<IHttpProxyFactory, DefaultHttpProxyFactory>();
            services.TryAddSingleton<IHttpProxyLoggerFactory, HttpProxyLoggerFactory>();
            
            services.TryAddTransient<IHttpProxyRotationFeature, RoundRobinHttpProxyRotationFeature>();
            services.TryAddTransient<IHttpProxyHealthCheckFeature, THealthCheckFeature>();
            
            services.TryAddTransient(typeof(IHttpProxyFeatureProvider<>), 
                typeof(TransparentHttpProxyFeatureServiceProvider<>));
            
            return services;
        }

        public static IServiceCollection AddDefaultHttpProxyClient<TProxyProvider>(
            this IServiceCollection services)
            where TProxyProvider : class, IHttpProxyDefinitionFactory
        {
            return services.AddDefaultHttpProxyClient<TProxyProvider, NullHttpProxyHealthCheckFeature>(_ => { });
        } 
        
        public static IServiceCollection AddDefaultHttpProxyClient<TProxyProvider,  THealthCheckFeature>(
            this IServiceCollection services)
            where TProxyProvider : class, IHttpProxyDefinitionFactory
            where THealthCheckFeature : class, IHttpProxyHealthCheckFeature
        {
            return services.AddDefaultHttpProxyClient<TProxyProvider, THealthCheckFeature>(_ => { });
        } 
    }
}