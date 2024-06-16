using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public class HostModeLoader : BaseManifestModeLoader
    {
        private string _taskGUID1 = string.Empty;
        private string _buildInPath = string.Empty;
        private NatAssetManifest _buildInManifest;

        private string _taskGUID2 = string.Empty;
        private string _readWritePath = string.Empty;
        private NatAssetManifest _readWriteManifest;

        private int _record = 0;

        internal override void StartLoader()
        {
            _record = 0;
            _buildInPath = Path.Combine(BuildInLoadPath, NatAssetSetting.ConfigName);
            _readWritePath = Path.Combine(ReadWriteLoadPath, NatAssetSetting.ConfigName);
            _taskGUID1 = CoreLoaderMgr.SendWebRequest(_buildInPath, Priority.Top, BuildInCallBack);
            _taskGUID2 = CoreLoaderMgr.SendWebRequest(_readWritePath, Priority.Top, ReadWriteCallBack);
        }

        internal override void StopLoader()
        {
            CoreLoaderMgr.DisposeWebRequest(_taskGUID1);
            CoreLoaderMgr.DisposeWebRequest(_taskGUID2);
            _buildInManifest = null;
            _readWriteManifest = null;
            _buildInPath = string.Empty;
            _readWritePath = string.Empty;
            _record = 0;
        }

        private void BuildInCallBack(string taskId, bool success, UnityWebRequest unityWebRequest)
        {
            _record++;
            if (success)
            {
                _buildInManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
                _buildInManifest.InitRunTimeManifest(LoadPath.ReadOnly);
            }
            else
            {
                Debug.LogError("不存在随包资源");
            }
            CheckFinish();
        }

        private void ReadWriteCallBack(string taskId, bool success, UnityWebRequest unityWebRequest)
        {
            _record++;
            if (success)
            {
                _readWriteManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
                _readWriteManifest.InitRunTimeManifest(LoadPath.ReadWrite);
            }
            else
            {
                Debug.LogError("不存在读写资源");
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
            if(_readWriteManifest != null)
                manifest = _readWriteManifest.GetBundleManifest(bundlePath);
            if(manifest != null)
                return manifest;
            if (_buildInManifest != null)
                manifest = _buildInManifest.GetBundleManifest(bundlePath);
            return manifest;
        }

        internal override AssetManifest GetAssetManifest(string assetPath)
        {
            AssetManifest manifest = null;
            if(_readWriteManifest != null)
                manifest = _readWriteManifest.GetAssetManifest(assetPath);
            if(manifest != null)
                return manifest;
            if (_buildInManifest != null)
                manifest = _buildInManifest.GetAssetManifest(assetPath);
            return manifest;
        }
        internal override List<string> GetBundleGroup(string groupName)
        {
            throw new System.NotImplementedException();
        }

        internal override Dictionary<string, BundleManifest> GetBundleManifestDic()
        {
            Dictionary<string, BundleManifest> resultDic = new Dictionary<string, BundleManifest>();
            if (_readWriteManifest != null)
            {
                foreach (var keyValue in _readWriteManifest.BundleManifestDic)
                {
                    resultDic.Add(keyValue.Key, keyValue.Value);
                }
            }
            if(_buildInManifest != null)
            {
                foreach(var keyvalue  in _buildInManifest.BundleManifestDic)
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
