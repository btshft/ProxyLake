using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ProxyLake.Http.Logging
{
    internal class HttpProxyLogger : ILogger
    {
        private readonly ILogger _logger;

        public HttpProxyLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                _logger.Log(logLevel, eventId, state, exception, (s, e) =>
                {
                    var message = formatter(s, e);
                    return string.IsNullOrEmpty(message)
                        ? message
                        : FormatMessage(message);
                });
            }
            else
            {
                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        private string FormatMessage(string message)
        {
            return $"Thread id: {Thread.CurrentThread.ManagedThreadId}{Environment.NewLine}Message: {message}";
        }
    }
}