using System;
using System.Collections.Generic;
using System.Linq;

namespace ProxyLake.Http.Features
{
    internal sealed class HttpProxyFeatureServiceProvider<TFeature>
        : IHttpProxyFeatureProvider<TFeature> where TFeature : class, IHttpProxyFeature
    {
        private readonly IEnumerable<HttpProxyFeatureFactory<TFeature>> _featureFactories;
        private readonly HttpProxyFallbackFeatureProvider<TFeature> _fallbackFeatureProvider;

        public HttpProxyFeatureServiceProvider(
            IEnumerable<HttpProxyFeatureFactory<TFeature>> featureFactories, 
            HttpProxyFallbackFeatureProvider<TFeature> fallbackFeatureProvider)
        {
            _featureFactories = featureFactories;
            _fallbackFeatureProvider = fallbackFeatureProvider;
        }

        /// <inheritdoc />
        public TFeature GetFeature(string clientName)
        {
            var factory = _featureFactories.FirstOrDefault(f =>
                string.Equals(f.Name, clientName, StringComparison.InvariantCultureIgnoreCase));

            return factory == null 
                ? _fallbackFeatureProvider.FeatureFactory() 
                : factory.FeatureProvider();
        }
    }
}