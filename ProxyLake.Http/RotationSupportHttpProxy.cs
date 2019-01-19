using System;
using System.Net;
using System.Threading;
using ProxyLake.Http.Abstractions;

namespace ProxyLake.Http
{
    internal class RotationSupportHttpProxy : IRotationSupportHttpProxy, IRentedHttpProxy
    {
        private readonly IHttpProxyRotationService _rotationService;
        private readonly IHttpProxyPool _httpProxyPool;

        private IRentedHttpProxy _currentProxy;
  
        /// <inheritdoc />
        public ICredentials Credentials
        {
            get => _currentProxy?.Credentials;
            set
            {
                if (_currentProxy != null)
                    _currentProxy.Credentials = value;
            }
        }

        /// <inheritdoc />
        public Guid Id => _currentProxy?.Id ?? Guid.Empty;

        /// <inheritdoc />
        public Uri Address => _currentProxy?.Address;

        /// <inheritdoc />
        public Guid RenterId => _currentProxy?.RenterId ?? Guid.Empty;
     
        public RotationSupportHttpProxy(IHttpProxyRotationService rotationService, IHttpProxyPool httpProxyPool)
        {
            _rotationService = rotationService;
            _httpProxyPool = httpProxyPool;
        }

        /// <inheritdoc />
        public bool TryRotate(CancellationToken cancellationToken)
        {
            if (_currentProxy == null)
            {
                _currentProxy = _httpProxyPool.Rent(cancellationToken);
                return _currentProxy != null;
            }
            
            if (!_rotationService.IsRotationRequired(_currentProxy)) 
                return true;
            
            if (_rotationService.TryRotate(_currentProxy, cancellationToken, out var newProxy))
            {
                Interlocked.Exchange(ref _currentProxy, newProxy);
                return true;
            }

            return false;

        }

        /// <inheritdoc />
        public Uri GetProxy(Uri destination)
        {
            return _currentProxy?.GetProxy(destination);
        }

        /// <inheritdoc />
        public bool IsBypassed(Uri host)
        {
            return _currentProxy?.IsBypassed(host) ?? false;
        }
        
        /// <inheritdoc />
        public void Return()
        {
            _currentProxy?.Return();
        }
    }
}