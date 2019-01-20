namespace ProxyLake.Http
{
    public interface IHttpProxyFactory
    {
        IHttpProxy CreateProxy(HttpProxyDefinition definition);
    }
}