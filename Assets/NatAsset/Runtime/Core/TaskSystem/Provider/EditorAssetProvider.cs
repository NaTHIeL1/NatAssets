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
        private Type _type;
        protected override void OnCreate()
        {
        }

        internal void SetAssetType(Type type)
        {
            _type = type;
        }

        internal override void OnUpdate()
        {
            if (ProviderState == ProviderState.Waiting)
            {
                Object obj = AssetDatabase.LoadAssetAtPath(ProviderGUID, _type);
                SetAssetSetting(obj);
                SetProviderState(ProviderState.Finish);
            }
        }

        protected override void OnClear()
        {
            _assetName = null;
            _type = null;
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
                error = $"资源路径:{ProviderGUID},加载资源资源名:{_assetName}时出错，检查是否资源名错误";
                SetAssetHandle(asset, error, null);
                SetProviderResult(ProviderResult.Faild);
                return;
            }
            
            AssetInfo assetInfo = AssetInfo.CreateAssetInfo(ProviderGUID, asset, null);
            RuntimeData.AddAssetInfo(assetInfo);
            SetAssetHandle(asset, error, assetInfo);
            SetProviderResult(ProviderResult.Success);
        }
        
        private void SetAssetHandle(object asset, string error, AssetInfo assetInfo)
        {
            if (error != String.Empty)
                Debug.LogErrorFormat(error);

            //当走完资源加载逻辑，但相应的发起句柄已全部卸载则立即启用资源卸载逻辑
            if (Handles == null || Handles.Count == 0)
            {
                assetInfo?.AddRefCount();
                assetInfo?.RedRefCount();
                return;
            }

            //正常情况下填充句柄
            for (int i = 0; i < Handles.Count; i++)
            {
                assetInfo?.AddRefCount();
                AssetHandle assetHandle = Handles[i] as AssetHandle;
                assetHandle.SetAsset(asset);
            }
        }
    }
#endif
}
