using System.Collections.Generic;

namespace ProxyLake.Http.Abstractions
{
    public interface IHttpProxyDefinitionProvider
    {
        IEnumerable<HttpProxyDefinition> GetProxyDefinitions();
    }
}