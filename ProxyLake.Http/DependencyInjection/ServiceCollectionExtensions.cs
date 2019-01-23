using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ProxyLake.Http.Features;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {       
        public static IHttpClientConfigurationSyntax AddDefaultHttpProxyClient<TProxyFactory>(
            this IServiceCollection services)
            where TProxyFactory : class, IHttpProxyDefinitionFactory
        {
            return services.AddHttpProxyClient<TProxyFactory>(Options.DefaultName);
        }

        public static IHttpClientConfigurationSyntax AddHttpProxyClient<TProxyFactory>(
            this IServiceCollection services, string name)
            where TProxyFactory : class, IHttpProxyDefinitionFactory
        {
            if (name == null)
                throw new ArgumentException(nameof(name));

            services.AddLogging();
            services.AddOptions();
            
            /* Core components */
            services.TryAddSingleton<IHttpProxyClientFactory, DefaultHttpProxyClientFactory>();
            services.TryAddSingleton<IHttpProxyDefinitionFactory, TProxyFactory>();
            services.TryAddSingleton<IHttpProxyFactory, DefaultHttpProxyFactory>();
            services.TryAddSingleton<IHttpProxyLoggerFactory, HttpProxyLoggerFactory>();
            
            /* Features */
            services
                .AddDefaultFeature<IHttpProxyRotationFeature, RoundRobinHttpProxyRotationFeature>()
                .AddDefaultFeature<IHttpProxyHealthCheckFeature, NullHttpProxyHealthCheck>()
                .AddDefaultFeature<IHttpProxyRefill, NullHttpProxyRefill>();
            
            services.TryAddTransient(typeof(IHttpProxyFeatureProvider<>), 
                typeof(HttpProxyFeatureServiceProvider<>));
            
            return new HttpClientConfigurationSyntax(name, services);
        }
        
        internal static IServiceCollection AddFeature<TFeature, TFeatureImpl>(
            this IServiceCollection serviceCollection, string name)
            where TFeature : class, IHttpProxyFeature
            where TFeatureImpl : class, TFeature
        {
            serviceCollection.TryAddTransient<TFeatureImpl>();
            serviceCollection.AddTransient<HttpProxyFeatureFactory<TFeature>>(p =>
            {
                var existingFactory = p.GetService<HttpProxyFeatureFactory<TFeature, TFeatureImpl>>();
                if (existingFactory != null && existingFactory.Name == name)
                    return existingFactory;
                    
                return new HttpProxyFeatureFactory<TFeature, TFeatureImpl>(
                    name, p.GetRequiredService<TFeatureImpl>);
            });
            
            return serviceCollection;
        }

        internal static IServiceCollection AddDefaultFeature<TFeature, TFeatureImpl>(this IServiceCollection services)
            where TFeature : class, IHttpProxyFeature
            where TFeatureImpl : class, TFeature
        {
            services.TryAddTransient<TFeatureImpl>();
            services.TryAddTransient<HttpProxyFallbackFeatureProvider<TFeature>>(
                provider =>
                {
                    return new HttpProxyFallbackFeatureProvider<TFeature>(
                        provider.GetRequiredService<TFeatureImpl>);
                });

            return services;
        }
    }
}