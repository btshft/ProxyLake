using Microsoft.Extensions.DependencyInjection;

namespace ProxyLake.Http.DependencyInjection
{
    internal class HttpClientConfigurationSyntax : IHttpClientConfigurationSyntax
    {
        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }

        public HttpClientConfigurationSyntax(string name, IServiceCollection services)
        {
            Name = name;
            Services = services;
        }
    }
}