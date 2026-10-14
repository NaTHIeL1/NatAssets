using System;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace NATFrameWork.NatAsset.Runtime
{
#if UNITY_EDITOR
    internal class EditorAssetProvider:BaseProvider
    {
        private string _assetName;
        private Type _assetType;
        private AssetProviderParam _assetProviderParam;
        private AssetLifetimeInfo _loadingLifetime;
        private string _loadError;
        protected override void OnCreate()
        {
            _assetProviderParam = (AssetProviderParam)_providerParam;
            _assetType = _assetProviderParam.AssetType;
            try { _loadingLifetime = RuntimeData.AcquireAssetLoad(AssetPath, true); }
            catch (Exception error) { _loadError = error.Message; }
        }

        internal override void OnUpdate()
        {
            if (_loadError != null)
            {
                SetProviderState(ProviderState.Finish);
                SetProviderResult(ProviderResult.Faild);
                CompleteAssetHandles(null, _loadError, null);
                return;
            }
            if (ProviderState == ProviderState.Waiting)
            {
                Object obj = AssetDatabase.LoadAssetAtPath(AssetPath, _assetType);
                SetProviderState(ProviderState.Finish);
                SetAssetSetting(obj);
            }
        }

        protected override void OnClear()
        {
            RuntimeData.ReleaseAssetLoad(_loadingLifetime);
            _loadingLifetime = null;
            _loadError = null;
            _assetName = null;
            _assetType = null;
            _assetProviderParam = default;
        }

        protected override void OnChangeLoadType(RunModel runModel)
        {
            if (ProviderState == ProviderState.Waiting)
            {
                OnUpdate();
                return;
            }
        }

        protected override void OnChangePriority(Priority targetPriority)
        {
        }
        
        private void SetAssetSetting(object asset)
        {
            string error = string.Empty;
            if (asset == null)
            {
                error = $"资源路径:{AssetPath},加载资源资源名:{_assetName}时出错，检查是否资源名错误";
                SetProviderResult(ProviderResult.Faild);
                SetAssetHandle(asset, error, null);
                return;
            }
            
            AssetInfo assetInfo = AssetInfo.CreateAssetInfo(AssetPath, _assetType, asset, null);
            RuntimeData.AddAssetInfo(assetInfo);
            SetProviderResult(ProviderResult.Success);
            SetAssetHandle(asset, error, assetInfo);
        }
        
        private void SetAssetHandle(object asset, string error, AssetInfo info)
        {
            CompleteAssetHandles(asset, error, info);
        }
    }
#endif
}
