using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace ProxyLake.Http
{
    public static class HttpProxyDefinitionProviderExtensions
    {
        public static IEnumerable<HttpProxyDefinition> GetProxyDefinitions(this IHttpProxyDefinitionProvider provider)
        {
            return provider.GetProxyDefinitions(Options.DefaultName);
        }
    }
}