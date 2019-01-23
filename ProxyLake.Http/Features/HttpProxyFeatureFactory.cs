using System;

namespace ProxyLake.Http.Features
{
    internal class HttpProxyFeatureFactory<TFeature>
        where TFeature : class, IHttpProxyFeature
    {
        public string Name { get; }
        public Func<TFeature> FeatureProvider { get; }
        
        public HttpProxyFeatureFactory(string name, Func<TFeature> featureProvider)
        {
            Name = name;
            FeatureProvider = featureProvider;
        }
    }
    
    // ReSharper disable once UnusedTypeParameter - Used for DI
    internal class HttpProxyFeatureFactory<TFeature, TFeatureImpl> : HttpProxyFeatureFactory<TFeature> 
        where TFeature : class, IHttpProxyFeature
        where TFeatureImpl : class, TFeature
    {
        public HttpProxyFeatureFactory(string name, Func<TFeature> featureProvider) 
            : base(name, featureProvider)
        {
        }
    }
}