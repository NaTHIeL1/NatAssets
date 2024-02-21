using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public class WebModeLoader : BaseManifestModeLoader
    {
        private string _buildInPath = string.Empty;
        private NatAssetManifest _buildInManifest;

        private string _remotePath = string.Empty;
        private NatAssetManifest _remoteManifest;

        private int _record = 0;
        internal override void StartLoader()
        {
            _record = 0;
            _buildInPath = Path.Combine(BuildInLoadPath, NatAssetSetting.ConfigName);
            _remotePath = Path.Combine(RemotePath, NatAssetSetting.ConfigName);
            CoreLoaderMgr.SendWebRequest(_buildInPath, Priority.Top, BuildInManifestCallBack, 1);
            CoreLoaderMgr.SendWebRequest(_remotePath, Priority.Top, RemoteManifestCallBack, 1);
        }
        internal override void StopLoader()
        {
            CoreLoaderMgr.DisposeWebRequest(_buildInPath);
            CoreLoaderMgr.DisposeWebRequest(_remotePath);
            _buildInManifest = null;
            _remoteManifest = null;
            _buildInPath = string.Empty;
            _remotePath = string.Empty;
            _record = 0;
        }

        private void BuildInManifestCallBack(bool success, UnityWebRequest unityWebRequest)
        {
            _record++;
            if (success)
            {
                _buildInManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
                _buildInManifest.InitRunTimeManifest(LoadPath.ReadOnly);
            }
            else
            {
                Debug.LogError("不存在随包配置文件");
            }
            CheckFinish();
        }

        private void RemoteManifestCallBack(bool success, UnityWebRequest unityWebRequest)
        {
            _record++;
            if (success)
            {
                _remoteManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
                _remoteManifest.InitRunTimeManifest(LoadPath.Remote);
            }
            else
            {
                Debug.LogError("不存在远程资源配置文件");
            }
            CheckFinish();
        }

        private void CheckFinish()
        {
            if(_record == 2)
            {
                LoadCallBack?.Invoke(ManifestModeState.Finish);
            }
        }


        internal override BundleManifest GetBundleManifest(string bundlePath)
        {
            BundleManifest manifest = null;
            if(_remoteManifest != null)
                manifest = _remoteManifest.GetBundleManifest(bundlePath);
            if (manifest != null)
                return manifest;
            if (_buildInManifest != null)
                manifest = _buildInManifest.GetBundleManifest(bundlePath);
            return manifest;
        }
        internal override AssetManifest GetAssetManifest(string assetPath)
        {
            AssetManifest manifest = null;
            if (_remoteManifest != null)
                manifest = _remoteManifest.GetAssetManifest(assetPath);
            if (manifest != null)
                return manifest;
            if (_buildInManifest != null)
                manifest = _buildInManifest.GetAssetManifest(assetPath);
            return manifest;
        }

        internal override List<string> GetBundleGroup(string groupName)
        {
            throw new NotImplementedException();
        }

        internal override Dictionary<string, BundleManifest> GetBundleManifestDic()
        {
            Dictionary<string, BundleManifest> resultDic = new Dictionary<string, BundleManifest>();
            if (_remoteManifest != null)
            {
                foreach (var keyValue in _remoteManifest.BundleManifestDic)
                {
                    resultDic.Add(keyValue.Key, keyValue.Value);
                }
            }
            if (_buildInManifest != null)
            {
                foreach (var keyvalue in _buildInManifest.BundleManifestDic)
                {
                    if (!resultDic.ContainsKey(keyvalue.Key))
                    {
                        resultDic.Add(keyvalue.Key, keyvalue.Value);
                    }
                }
            }
            return resultDic;
        }
    }
}
