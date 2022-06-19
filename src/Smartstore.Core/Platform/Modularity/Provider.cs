﻿namespace Smartstore.Engine.Modularity
{
    public sealed class Provider<TProvider> where TProvider : IProvider
    {
        private readonly Lazy<TProvider, ProviderMetadata> _lazy;

        public Provider(Lazy<TProvider, ProviderMetadata> lazy)
        {
            _lazy = lazy;
        }

        public TProvider Value => _lazy.Value;

        public ProviderMetadata Metadata => _lazy.Metadata;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public Lazy<TProvider, ProviderMetadata> ToLazy()
        {
            return _lazy;
        }

        public override string ToString()
        {
            return _lazy.Metadata.SystemName;
        }
    }
}
