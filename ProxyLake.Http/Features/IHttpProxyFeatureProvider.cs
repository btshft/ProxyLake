namespace ProxyLake.Http.Features
{
    internal interface IHttpProxyFeatureProvider<out TFeature> 
        where TFeature : IHttpProxyFeature
    {
        TFeature GetFeature(string clientName);
    }
}