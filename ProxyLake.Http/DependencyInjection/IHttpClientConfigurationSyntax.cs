using Microsoft.Extensions.DependencyInjection;

namespace ProxyLake.Http.DependencyInjection
{
    public interface IHttpClientConfigurationSyntax
    {
        /// <summary>
        /// Client name.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Serive collection;
        /// </summary>
        IServiceCollection Services { get; }
    }
}