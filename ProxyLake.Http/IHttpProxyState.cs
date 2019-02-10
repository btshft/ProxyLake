namespace ProxyLake.Http
{
    /// <summary>
    /// Describes proxy state.
    /// </summary>
    public interface IHttpProxyState
    {
        /// <summary>
        /// Current active proxy.
        /// </summary>
        IHttpProxy Proxy { get; }
        
        /// <summary>
        /// True if proxy acquired by someone else. 
        /// </summary>
        bool IsProxyAcquired { get; }
    }
}