using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class ManifestLoad
    {
        private static NatAssetManifest _buildInManifest;
        private static NatAssetManifest _readWriteManifest;

        private static int _loadCount = 0;
        private static Action<NatAssetManifest, NatAssetManifest> _loadCallback;

        internal static void Release()
        {
            _loadCallback = null;
            _buildInManifest = null;
            _readWriteManifest = null;
            _loadCount = 0;
        }
        internal static string BuildInLoadPath
        {
            get
            {
#if UNITY_EDITOR
                return NatAssetSetting.EditorLoadPath;
#endif
                return NatAssetSetting.ReadOnlyPath;
            }
        }

        internal static string ReadWriteLoadPath
        {
            get
            {
                return NatAssetSetting.ReadWritePath;
            }
        }

        internal static string RemotePath
        {
            get
            {
                return NatAssetSetting.RemotePath;
            }
        }
        internal static void LoadManifest(string configName, Action<NatAssetManifest, NatAssetManifest> callback = null)
        {
            _loadCallback = callback;
            string buildInPath = Path.Combine(BuildInLoadPath, configName);
            string readWritePath = Path.Combine(ReadWriteLoadPath, configName);
            //string remotePath = Path.Combine(RemotePath, configName);

            CoreLoaderMgr.SendWebRequest(buildInPath, Priority.Top, BuildInManifestCallBack, 1);
            CoreLoaderMgr.SendWebRequest(readWritePath, Priority.Top, ReadWriteManifestCallback, 1);
        }

        private static void BuildInManifestCallBack(string taskId, bool success, UnityWebRequest unityWebRequest)
        {
            _loadCount++;
            if (success)
            {
                _buildInManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
            }
            else
            {
                throw new System.Exception("不存在随包配置文件");
            }
            CheckAllManifestHasLoad();
        }

        private static void ReadWriteManifestCallback(string taskId, bool success, UnityWebRequest unityWebRequest)
        {
            _loadCount++;
            if (success)
            {
                _readWriteManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
            }
            CheckAllManifestHasLoad();
        }

        private static void CheckAllManifestHasLoad()
        {
            if (_loadCount == 2 && _loadCallback != null)
                _loadCallback.Invoke(_buildInManifest, _readWriteManifest);
        }
    }
}