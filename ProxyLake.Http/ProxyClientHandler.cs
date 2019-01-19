using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProxyLake.Http
{
    internal class ProxyClientHandler : HttpClientHandler
    {
        private readonly RotationSupportHttpProxy _rotationHttpProxy;
        private readonly ILogger _logger;

        public ProxyClientHandler(ILoggerFactory loggerFactory, RotationSupportHttpProxy rotationHttpProxy)
        {
            _rotationHttpProxy = rotationHttpProxy;
            _logger = loggerFactory.CreateLogger(typeof(ProxyClientHandler));
            UseProxy = true;
            Proxy = _rotationHttpProxy;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var previousProxyId = _rotationHttpProxy.Id;
                if (!_rotationHttpProxy.TryRotate(cancellationToken))
                {
                    // Log or throw or cancel request
                    _logger.LogWarning($"Proxy rotation failed. Current proxy id: '{_rotationHttpProxy.Id}'");
                }
                else
                {
                    _logger.LogDebug($"Proxy rotation succeed. From '{previousProxyId}' to '{_rotationHttpProxy.Id}'.");
                }

                return await base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            finally
            {
                _rotationHttpProxy.Return();
            }
        }
    }
}