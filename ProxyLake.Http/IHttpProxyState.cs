namespace ProxyLake.Http
{
    public interface IHttpProxyState
    {
        IHttpProxy Proxy { get; }
        bool IsProxyAcquired { get; }
    }
}