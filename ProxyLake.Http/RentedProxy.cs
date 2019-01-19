using System;
using System.Net;
using ProxyLake.Http.Abstractions;

namespace ProxyLake.Http
{
    internal class RentedProxy : IRentedHttpProxy
    {
        private readonly IHttpProxyPool _proxyPool;
        private readonly IHttpProxy _innerProxy;
        
        public RentedProxy(IHttpProxy innerProxy, IHttpProxyPool proxyPool)
        {
            _proxyPool = proxyPool;
            _innerProxy = innerProxy;
        }

        /// <inheritdoc />
        public Guid RenterId { get; set; }

        /// <inheritdoc />
        public ICredentials Credentials
        {
            get => _innerProxy.Credentials;
            set => _innerProxy.Credentials = value;
        }

        /// <inheritdoc />
        public Guid Id => _innerProxy.Id;

        /// <inheritdoc />
        public Uri Address => _innerProxy.Address;
        
        /// <inheritdoc />
        public void Return()
        {
            _proxyPool.Return(this);
        }

        /// <inheritdoc />
        public Uri GetProxy(Uri destination)
        {
            return _innerProxy.GetProxy(destination);
        }

        /// <inheritdoc />
        public bool IsBypassed(Uri host)
        {
            return _innerProxy.IsBypassed(host);
        }
    }
}