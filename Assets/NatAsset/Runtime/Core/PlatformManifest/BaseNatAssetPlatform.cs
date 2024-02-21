//using Cysharp.Threading.Tasks;
//using System;
//using System.IO;
//using UnityEngine.Networking;

//namespace NATFrameWork.NatAsset.Runtime
//{
//    public abstract class BaseNatAssetPlatform
//    {
//        private NatAssetManifest _buildInManifest;
//        private NatAssetManifest _readWriteManifest;

//        private NatAssetManifest _resultManifest;
//        private int _loadCount = 0;
//        private Action<NatAssetManifest> _loadCallback;
//        protected virtual string BuildInLoadPath
//        {
//            get
//            {
//                return NatAssetSetting.ReadOnlyPath;
//            }
//        }

//        protected virtual string ReadWriteLoadPath
//        {
//            get
//            {
//                return NatAssetSetting.ReadWritePath;
//            }
//        }
//        internal void LoadManifest(string configName, Action<NatAssetManifest> callback = null)
//        {
//            _loadCallback = callback;
//            string buildInPath = Path.Combine(BuildInLoadPath, configName);
//            string readWritePath = Path.Combine(ReadWriteLoadPath, configName);
//            CoreLoaderMgr.SendWebRequest(buildInPath, Priority.Top, BuildInManifestCallBack, 1);
//            CoreLoaderMgr.SendWebRequest(readWritePath, Priority.Top, ReadWriteManifestCallback, 1);
//        }
//        public abstract UniTask<NatAssetManifest> LoadReadPathNatManifest(string ConfigName);

//        public abstract UniTask<NatAssetManifest> LoadWritePathNatManifest(string ConfigName);

//        private void BuildInManifestCallBack(bool success, UnityWebRequest unityWebRequest)
//        {
//            _loadCount++;
//            if (success)
//            {
//                _buildInManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
//            }
//            else
//            {
//                throw new System.Exception("不存在随包配置文件");
//            }
//            CheckAllManifestHasLoad();
//        }

//        private void ReadWriteManifestCallback(bool success, UnityWebRequest unityWebRequest)
//        {
//            _loadCount++;
//            if (success)
//            {
//                _readWriteManifest = NatAssetManifest.DeserializeFromBinary(unityWebRequest.downloadHandler.data);
//            }
//            CheckAllManifestHasLoad();
//        }

//        private void CheckAllManifestHasLoad()
//        {
//            if (_loadCount == 2 && _loadCallback != null)
//                _loadCallback.Invoke(_resultManifest);
//        }
//    }
//}