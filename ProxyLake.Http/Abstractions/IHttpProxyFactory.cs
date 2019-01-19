namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyFactory
    {
        IHttpProxy CreateProxy(HttpProxyDefinition definition);
    }
}