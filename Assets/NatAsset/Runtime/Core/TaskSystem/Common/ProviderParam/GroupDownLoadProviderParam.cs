namespace NATFrameWork.NatAsset.Runtime
{
    public struct GroupDownLoadProviderParam : IProviderParam
    {
        public string ProviderGUID => _providerGUID;
        public string AssetPath => _assetPath;
        public NatUpdaterInfo NatUpdaterInfo => _updateInfo;

        private string _providerGUID;
        private string _assetPath;
        private NatUpdaterInfo _updateInfo;

        public GroupDownLoadProviderParam(string providerGUID, string providerName, NatUpdaterInfo natUpdaterInfo)
        {
            _providerGUID = providerGUID;
            _assetPath = providerName;
            _updateInfo = natUpdaterInfo;
        }
    }
}
