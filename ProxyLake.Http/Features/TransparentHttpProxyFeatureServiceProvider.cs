namespace ProxyLake.Http.Features
{
    internal sealed class TransparentHttpProxyFeatureServiceProvider<TFeature>
        : IHttpProxyFeatureProvider<TFeature> where TFeature : IHttpProxyFeature
    {
        private readonly TFeature _feature;

        public TransparentHttpProxyFeatureServiceProvider(TFeature feature)
        {
            _feature = feature;
        }

        /// <inheritdoc />
        public TFeature GetFeature(string clientName)
        {
            return _feature;
        }
    }
}