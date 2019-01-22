using System;
using System.Net.Http;

namespace ProxyLake.Http
{
    internal class DeadHandlerReference
    {
        private readonly WeakReference _handlerTracker;
        private readonly WeakReference<IHttpProxy> _proxyHolder;

        public DeadHandlerReference(HttpProxyHandlerState state)
        {
            _handlerTracker = new WeakReference(state.Handler);
            _proxyHolder = new WeakReference<IHttpProxy>(state.ProxyState.Proxy);
            Inner = state.Handler.InnerHandler;
        }
        
        public HttpMessageHandler Inner { get; }

        public IHttpProxy Proxy => _proxyHolder.TryGetTarget(out var proxy) ? proxy : null;
        
        public bool IsCollectedByGC => !_handlerTracker.IsAlive;
    }
}