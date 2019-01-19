using Microsoft.Extensions.Logging;

namespace ProxyLake.Http.Logging
{
    internal class ProxyLoggerFactory : IHttpProxyLoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public ProxyLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new HttpProxyLogger(_loggerFactory.CreateLogger(categoryName));
        }

        /// <inheritdoc />
        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            _loggerFactory?.Dispose();
        }
    }
}