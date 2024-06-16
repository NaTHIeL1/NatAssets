using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public class StandaloneModeLoader : BaseManifestModeLoader
    {
        private string _webTaskGUID = string.Empty;
        private string _buildInPath = string.Empty;
        private NatAssetManifest _buildInManifest;
        
        internal override void StartLoader()
        {
            _buildInPath = Path.Combine(BuildInLoadPath, NatAssetSetting.ConfigName);
            _webTaskGUID = CoreLoaderMgr.SendWebRequest(_buildInPath, Priority.Top, CallBack);
        }

        private void CallBack(string taskId, bool success, UnityWebRequest unityWebRequest)
        {
            if(success)
            {
                _buildInManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
                _buildInManifest.InitRunTimeManifest(LoadPath.ReadOnly);
            }
            else
            {
                Debug.LogError("不存在随包资源");
            }

            //todo:获取bunldeManifest后重新构建其对应的框架
            LoadCallBack?.Invoke(ManifestModeState.Finish);
        }

        internal override void StopLoader()
        {
            //不论如何确保停止了该任务
            CoreLoaderMgr.DisposeWebRequest(_webTaskGUID);
            _buildInPath = string.Empty;
            _buildInManifest = null;
        }
        
        internal override BundleManifest GetBundleManifest(string bundlePath)
        {
            if(_buildInManifest != null)
            {
                return _buildInManifest.GetBundleManifest(bundlePath);
            }
            return null;
        }

        internal override AssetManifest GetAssetManifest(string assetPath)
        {
            if(_buildInManifest != null)
            {
                return _buildInManifest.GetAssetManifest(assetPath);
            }
            return null;
        }

        internal override List<string> GetBundleGroup(string groupName)
        {
            throw new System.NotImplementedException();
        }

        internal override Dictionary<string, BundleManifest> GetBundleManifestDic()
        {
            if (_buildInManifest != null)
                return _buildInManifest.BundleManifestDic;
            return null;
        }
    }
}
