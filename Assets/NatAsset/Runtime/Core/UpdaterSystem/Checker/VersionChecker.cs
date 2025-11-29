using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class VersionChecker
    {
        private static string _taskRemote = string.Empty;
        private static string _taskReadOnly = string.Empty;
        private static string _taskReadWrite = string.Empty;

        private static bool remoteLoaded = false;
        private static bool readOnlyLoaded = false;
        private static bool readWriteLoaded = false;

        private static bool _isChecking = false;
        private static ulong _mainfestLength = 0;
        private static NatAssetManifest _remoteNatAssetManifest = null;
        private static Action<NatUpdaterInfo> _versionCallBack = null;

        private static Dictionary<string, CheckInfo> _checkInfoDic = new Dictionary<string, CheckInfo>();
        internal static void CheckVersion(Action<NatUpdaterInfo> VersionCallBack)
        {
            if(_isChecking) 
                return;
            _isChecking = true;
            _versionCallBack = VersionCallBack;
            _mainfestLength = 0;
            _remoteNatAssetManifest = null;

            string remotePath = Path.Combine(NatAssetSetting.AssetServerURL, NatAssetSetting.ConfigName);
            string readOnlyPath = Path.Combine(NatAssetSetting.ReadOnlyPath, NatAssetSetting.ConfigName);
            string readWritePath = Path.Combine(NatAssetSetting.ReadWritePath, NatAssetSetting.ConfigName);

            _taskRemote = CoreLoaderMgr.SendWebRequest(remotePath, Priority.Top, RemoteCallBack);
            _taskReadOnly = CoreLoaderMgr.SendWebRequest(readOnlyPath, Priority.Top, ReadOnlyCallBack);
            _taskReadWrite = CoreLoaderMgr.SendWebRequest(readWritePath, Priority.Top, ReadWriteCallBack);
        }

        internal static void Clear()
        {
            CoreLoaderMgr.DisposeWebRequest( _taskRemote );
            CoreLoaderMgr.DisposeWebRequest(_taskReadOnly );
            CoreLoaderMgr.DisposeWebRequest(_taskReadWrite );

            _mainfestLength = 0;
            _remoteNatAssetManifest = null;
            _isChecking = false;
            _versionCallBack = null;
            _checkInfoDic.Clear();

            _taskRemote = string.Empty;
            _taskReadOnly = string.Empty;
            _taskReadWrite = string.Empty;
            remoteLoaded = false;
            readOnlyLoaded = false;
            readWriteLoaded = false;
        }

        private static void ReadWriteCallBack(string taskID, bool success, UnityWebRequest request)
        {
            if (taskID != _taskReadWrite)
                return;
            
            if(!success || request.downloadHandler.data == null)
            {
                readWriteLoaded = true;
                Debug.LogError($"未加载到读写区资源：{request.error}");
                CheckComplete();
                return;
            }

            NatAssetManifest natAssetManifest = NatAssetManifest.DeserializeFromBinary(request.downloadHandler.data);
            if (natAssetManifest == null)
            {
                ManifestLoadFail("读写区配置序列化失败");
                return;
            }

            List<BundleManifest> bundles = natAssetManifest.BundleManifests;
            for(int  i = 0; i < bundles.Count; i++)
            {
                BundleManifest bundleManifest = bundles[i];
                CheckInfo temp = GetOrAddCheckInfo(bundleManifest.BundlePath);
                temp.ReadWriteManifest = bundleManifest;
            }

            readWriteLoaded = true;
            CheckComplete();
        }

        private static void ReadOnlyCallBack(string taskID, bool success, UnityWebRequest request)
        {
            if (taskID != _taskReadOnly)
                return;
            
            if(!success || request.downloadHandler.data == null)
            {
                readOnlyLoaded = true;
                Debug.LogError($"未加载到只读取资源：{request.error}");
                CheckComplete();
                return;
            }

            NatAssetManifest natAssetManifest = NatAssetManifest.DeserializeFromBinary(request.downloadHandler.data);
            if (natAssetManifest == null)
            {
                ManifestLoadFail("只读区配置序列化失败");
                return;
            }

            List<BundleManifest> bundles = natAssetManifest.BundleManifests;
            for(int i = 0; i < bundles.Count; i++)
            {
                BundleManifest bundleManifest = bundles[i];
                CheckInfo temp = GetOrAddCheckInfo(bundleManifest.BundlePath);
                temp.ReadOnlyManifest = bundleManifest;
            }

            readOnlyLoaded = true;
            CheckComplete();
        }

        private static void RemoteCallBack(string taskID, bool success, UnityWebRequest request)
        {
            if (taskID != _taskRemote)
                return;
            
            if (!success || request.downloadHandler.data == null)
            {
                Debug.LogError($"未加载到远端资源：{request.error}");
                NatUpdaterInfo natUpdaterInfo = new NatUpdaterInfo(success, request.error);
                remoteLoaded = true;
                _versionCallBack?.Invoke(natUpdaterInfo);
                Clear();
                return;
            }

            _mainfestLength = (ulong)request.downloadHandler.data.Length;
            NatAssetManifest natAssetManifest = NatAssetManifest.DeserializeFromBinary(request.downloadHandler.data);
            _remoteNatAssetManifest = natAssetManifest;
            if(natAssetManifest == null)
            {
                ManifestLoadFail("远端配置序列化失败");
                return;
            }

            List<BundleManifest> bundles = natAssetManifest.BundleManifests;
            for(int i = 0; i < bundles.Count; i++)
            {
                BundleManifest bundleManifest = bundles[i];
                CheckInfo temp = GetOrAddCheckInfo(bundleManifest.BundlePath);
                temp.RemoteManifest = bundleManifest;
            }

            remoteLoaded = true;
            CheckComplete();
        }

        private static void CheckComplete()
        {
            if (remoteLoaded && readOnlyLoaded && readWriteLoaded)
            {
                //全部文件加载完毕,刷新更新检查
                RefreshCheckInfo();

                NatUpdaterInfo natUpdaterInfo = new NatUpdaterInfo(true, string.Empty);
                natUpdaterInfo.manifestLength = _mainfestLength;
                if(_remoteNatAssetManifest != null)
                    natUpdaterInfo.SetRemoteManifest(_remoteNatAssetManifest);
                //检测资源是否需要更新
                foreach (var kv in _checkInfoDic)
                {
                    CheckInfo temp = kv.Value;
                    //检测到需要下载和更新的部分并存储
                    if(temp.HasDifference())
                    {
                        natUpdaterInfo.AddCheckInfo(temp);
                    }
                }
                _versionCallBack?.Invoke(natUpdaterInfo);
                Clear();
            }
        }

        private static void ManifestLoadFail(string error)
        {
            Debug.LogError(error);
            NatUpdaterInfo updaterInfo = new NatUpdaterInfo(false, error);
            _versionCallBack?.Invoke(updaterInfo);
            Clear();
        }

        //todo:生成和刷新CheckInfo
        private static CheckInfo GetOrAddCheckInfo(string checkName)
        {
            if(!_checkInfoDic.TryGetValue(checkName, out var info))
            {
                CheckInfo newInfo = new CheckInfo(checkName);
                _checkInfoDic.Add(checkName, newInfo);
                info = newInfo;
            }
            return info;
        }

        private static void RefreshCheckInfo()
        {
            if (_checkInfoDic != null)
            {
                foreach (var kvp in _checkInfoDic)
                {
                    var info = kvp.Value;
                    info.CheckUpadte();
                }
            }
        }
    }
}
