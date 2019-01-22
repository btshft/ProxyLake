namespace ProxyLake.Http.Features
{
    public interface IHttpProxyFeatureProvider<out TFeature> 
        where TFeature : IHttpProxyFeature
    {
        TFeature GetFeature(string clientName);
    }
}