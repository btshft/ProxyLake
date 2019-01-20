using System.Threading;

namespace ProxyLake.Http
{
    internal sealed class HttpProxyState : IHttpProxyState
    {
        private bool _isProxyAcquired;

        public IHttpProxy Proxy { get; }

        public bool IsProxyAcquired => _isProxyAcquired;

        public HttpProxyState(IHttpProxy proxy)
        {
            Proxy = proxy;
        }
        
        public bool TryAcquireProxy()
        {
            if (Volatile.Read(ref _isProxyAcquired))
                return false;

            Volatile.Write(ref _isProxyAcquired, true);
            return true;
        }
        
        public bool TryReleaseProxy()
        {
            if (!Volatile.Read(ref _isProxyAcquired))
                return false;

            Volatile.Write(ref _isProxyAcquired, false);
            return true;
        }
    }
}