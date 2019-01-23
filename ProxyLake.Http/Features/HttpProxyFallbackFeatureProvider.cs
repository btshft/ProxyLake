using System;

namespace ProxyLake.Http.Features
{
    internal class HttpProxyFallbackFeatureProvider<TFeature>
        where TFeature : class, IHttpProxyFeature
    {
        public Func<TFeature> FeatureFactory { get; }
        
        public HttpProxyFallbackFeatureProvider(Func<TFeature> featureFactory)
        {
            FeatureFactory = featureFactory;
        }
    }
}