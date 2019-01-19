namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyHealthChecker
    {
        bool CanCheck(IHttpProxy proxy);
        bool IsAlive(IHttpProxy proxy);
    }
}