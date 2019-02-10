using System;

namespace ProxyLake.Http
{
    /// <summary>
    /// Http proxy definition.
    /// </summary>
    public class HttpProxyDefinition : IEquatable<HttpProxyDefinition>
    {
        /// <summary>
        /// Host.
        /// </summary>
        public string Host { get; set; }
        
        /// <summary>
        /// Port.
        /// </summary>
        public int? Port { get; set; }
        
        /// <summary>
        /// Should bypass proxy on local.
        /// </summary>
        public bool BypassOnLocal { get; set; }
        
        /// <summary>
        /// Credentials - username.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Credentials - password.
        /// </summary>
        public string Password { get; set; }

        /// <inheritdoc />
        public bool Equals(HttpProxyDefinition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Host, other.Host, StringComparison.InvariantCultureIgnoreCase) && Port == other.Port;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HttpProxyDefinition) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Host != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(Host) : 0) * 397) ^ Port.GetHashCode();
            }
        }
    }
}