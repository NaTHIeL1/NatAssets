using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal struct SceneProviderParam : IProviderParam
    {
        public string ProviderGUID => _providerGUID;

        public string AssetPath => _assetPath;

        public LoadSceneMode LoadSceneMode => _loadSceneMode;

        private string _providerGUID;
        private string _assetPath;
        private LoadSceneMode _loadSceneMode;

        public SceneProviderParam(string guid, string assetPath, LoadSceneMode loadSceneMode)
        {
            _providerGUID = guid;
            _assetPath = assetPath;
            _loadSceneMode = loadSceneMode;
        }
    }
}
