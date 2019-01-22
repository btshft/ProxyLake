using System.Collections.Generic;
using System.Threading;

namespace ProxyLake.Http
{ 
    public interface IHttpProxyDefinitionFactory
    {
        IReadOnlyCollection<HttpProxyDefinition> CreateDefinitions(string name, CancellationToken cancellation);
    }
}