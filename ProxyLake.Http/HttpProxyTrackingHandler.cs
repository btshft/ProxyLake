using System.Net.Http;

namespace ProxyLake.Http
{
    internal class HttpProxyTrackingHandler : DelegatingHandler
    {
        public HttpProxyTrackingHandler(HttpMessageHandler inner)
            : base(inner)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // Handled manually
        }
    }
}