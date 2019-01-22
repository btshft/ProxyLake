using System.Collections.Generic;
using ProxyLake.Http.Utils;

namespace ProxyLake.Http.Extensions
{
    internal static class CollectionExtensions
    {
        public static CopyOnWriteCollection<T> AsCopyOnWriteCollection<T>(this IReadOnlyCollection<T> collection)
        {
            return new CopyOnWriteCollection<T>(collection);
        }
    }
}