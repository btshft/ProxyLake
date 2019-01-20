using System.Collections.Generic;

namespace ProxyLake.Http
{
    public interface IHttpProxyDefinitionProvider
    {
        IEnumerable<HttpProxyDefinition> GetProxyDefinitions(string name);
    }
}