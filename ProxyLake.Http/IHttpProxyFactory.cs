namespace ProxyLake.Http
{
    /// <summary>
    /// Component to create proxies from definitions.
    /// </summary>
    public interface IHttpProxyFactory
    {
        /// <summary>
        /// Creates a new instance of http proxy.
        /// </summary>
        /// <param name="definition">Proxy definition.</param>
        /// <returns>Proxy instance.</returns>
        IHttpProxy CreateProxy(HttpProxyDefinition definition);
    }
}