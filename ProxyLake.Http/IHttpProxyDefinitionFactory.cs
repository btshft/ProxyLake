using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyLake.Http
{ 
    /// <summary>
    /// Component to provide proxy definitions.
    /// </summary>
    public interface IHttpProxyDefinitionFactory
    {
        /// <summary>
        /// Creates a set of proxy definitions for named client.
        /// </summary>
        /// <param name="name">Http proxy client name.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>A set of proxy definitions.</returns>
        Task<IReadOnlyCollection<HttpProxyDefinition>> CreateDefinitionsAsync(
            string name, CancellationToken cancellation);
    }
}