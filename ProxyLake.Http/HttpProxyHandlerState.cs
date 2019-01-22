using System;
using System.Net.Http;

namespace ProxyLake.Http
{
    internal sealed class HttpProxyHandlerState
    {
        private readonly Lazy<HttpProxyTrackingHandler> _handlerFactory;
        public HttpProxyTrackingHandler Handler => _handlerFactory.Value;
        public HttpProxyState ProxyState { get; }

        public HttpProxyHandlerState(HttpProxyState proxyState)
        {
            ProxyState = proxyState;
            _handlerFactory = new Lazy<HttpProxyTrackingHandler>(CreateHandler);
        }

        private HttpProxyTrackingHandler CreateHandler()
        {
            return new HttpProxyTrackingHandler(new HttpClientHandler
            {
                Proxy = ProxyState.Proxy,
                UseProxy = true
            });
        }
    }
}