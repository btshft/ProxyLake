using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ProxyLake.Http.Logging;

namespace ProxyLake.Http
{
    internal class HttpProxyClient : HttpClient
    {
        private readonly WeakReference<HttpProxyState> _httpProxyState;
        private readonly ILogger _logger;
        
        public HttpProxyClient(HttpProxyHandlerState state, IHttpProxyLoggerFactory loggerFactory)
            : base(state.Handler, disposeHandler: false)
        {
            _httpProxyState = new WeakReference<HttpProxyState>(
                state.ProxyState);
            _logger = loggerFactory.CreateLogger($"{nameof(HttpProxyClient)}");
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                if (disposing)
                {
                    if (_httpProxyState.TryGetTarget(out var state))
                    {
                        var isRelease = state.TryReleaseProxy();
                        _logger.LogDebug(
                            $"Dispose: proxy '{state.Proxy.Id}'{(isRelease ? "" : " not")} released");
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Dispose: Unable to get proxy state to release. Reference already collected");
                    }
                }
            }
        }
    }
}