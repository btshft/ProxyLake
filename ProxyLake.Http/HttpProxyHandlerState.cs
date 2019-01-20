using System;
using System.Net.Http;

namespace ProxyLake.Http
{
    internal sealed class HttpProxyHandlerState
    {
        private readonly Lazy<HttpClientHandler> _handlerFactory;

        public bool IsHandlerCreated => _handlerFactory.IsValueCreated;
        public HttpClientHandler Handler => _handlerFactory.Value;
        public HttpProxyState ProxyState { get; }

        public HttpProxyHandlerState(HttpProxyState proxyState)
        {
            ProxyState = proxyState;
            _handlerFactory = new Lazy<HttpClientHandler>(CreateHandler);
        }

        private HttpClientHandler CreateHandler()
        {
            return new HttpClientHandler
            {
                Proxy = ProxyState.Proxy,
                UseProxy = true
            };
        }
    }
}