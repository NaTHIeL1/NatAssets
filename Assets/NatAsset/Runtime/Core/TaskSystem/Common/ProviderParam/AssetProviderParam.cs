using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal struct AssetProviderParam : IProviderParam
    {
        public string ProviderGUID { get => _providerGUID; }
        public string AssetPath { get => _assetPath; }
        public Type AssetType { get => _assetType; }

        private string _providerGUID;
        private string _assetPath;
        private Type _assetType;

        public AssetProviderParam(string assetPath, Type assetType)
        {
            _providerGUID = TypeStringGUIDPool.GetTargetGUID(assetType, assetPath); ;
            _assetPath = assetPath;
            _assetType = assetType;
        }
    }
}
