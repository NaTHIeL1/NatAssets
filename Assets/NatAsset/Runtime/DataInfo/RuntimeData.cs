using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static partial class RuntimeData
    {
#if UNITY_EDITOR
        public static Dictionary<string, AssetInfo> AssetDic => _assetDic;
        public static Dictionary<string, BundleInfo> BundleDic => _bundleDic;
        public static Dictionary<int, SceneInfo> SceneDic => _sceneDic;
#endif
        //资源字典
        private static Dictionary<string, AssetInfo> _assetDic = new Dictionary<string, AssetInfo>(100);

        //Bundle字典
        private static Dictionary<string, BundleInfo> _bundleDic = new Dictionary<string, BundleInfo>(100);

        //场景字典
        private static Dictionary<int, SceneInfo> _sceneDic = new Dictionary<int, SceneInfo>(5);

        internal static event Action CompleteCallBack;

        private static NatAssetState _natAssetState = NatAssetState.Waiting;

        public static NatAssetState NatAssetState
        {
            get { return _natAssetState; }
        }

        private static bool CanInit => NatAssetState != NatAssetState.Release;

        internal static void Init(BaseManifestModeLoader modelLoader)
        {
            _manifestLoader = modelLoader;
            _natAssetState = NatAssetState.Waiting;
#if UNITY_EDITOR
            switch (NatAssetSetting.TRunWay)
            {
                case RunWay.Editor:
                    EditorInit();
                    break;
                case RunWay.PackageOnly:
                    RunTimeInit();
                    break;
            }
#else
            RunTimeInit();
#endif
        }

        internal static void EditorInit()
        {
            ExeCompleteCallbak();
        }

        internal static void RunTimeInit()
        {
            void Callback(ManifestModeState manifestModeState)
            {
                if (manifestModeState == ManifestModeState.Finish)
                {
                    ExeCompleteCallbak();
                }
            }
            _manifestLoader.LoadCallBack = Callback;
            _manifestLoader.StartLoader();
        }
        
        private static void ExeCompleteCallbak()
        {
            if (!CanInit)
            {
                if (_natAssetState == NatAssetState.Release)
                    CompleteCallBack = null;
                return;
            }

            _natAssetState = NatAssetState.Inited;
            CompleteCallBack?.Invoke();
            CompleteCallBack = null;
        }

        internal static void Release()
        {
            _manifestLoader.StopLoader();
            _natAssetState = NatAssetState.Release;
            _assetDic.Clear();
            _sceneDic.Clear();
            _bundleDic.Clear();
            //UnLoadMainfest();
        }

        //对回收的资源进行标记
        internal static void MarkAndLaunchUnLoad()
        {
            foreach (KeyValuePair<string, AssetInfo> keyValuePair in _assetDic)
                keyValuePair.Value.CollectionMark();
            foreach (KeyValuePair<int, SceneInfo> keyValuePair in _sceneDic)
                keyValuePair.Value.CollectionMark();

            foreach (KeyValuePair<string, AssetInfo> keyValuePair in _assetDic)
                keyValuePair.Value.RedRefCount();
            foreach (KeyValuePair<int, SceneInfo> keyValuePair in _sceneDic)
                keyValuePair.Value.RedRefCount();
        }

        //internal static 
        internal static AssetInfo GetAsset(string path)
        {
            if (_assetDic.ContainsKey(path))
            {
                return _assetDic[path];
            }

            return null;
        }

        internal static BundleInfo GetBundle(string path)
        {
            if (_bundleDic.ContainsKey(path))
            {
                return _bundleDic[path];
            }

            return null;
        }

        internal static SceneInfo GetScene(Scene scene)
        {
            if (_sceneDic.ContainsKey(scene.handle))
            {
                return _sceneDic[scene.handle];
            }

            return null;
        }

        internal static void AddAssetInfo(AssetInfo assetInfo)
        {
            string path = assetInfo.InfoNameGUID;
            if (!_assetDic.ContainsKey(path))
            {
                _assetDic.Add(path, assetInfo);
            }
        }

        internal static void RemoveAssetInfo(AssetInfo assetInfo)
        {
            string path = assetInfo.InfoNameGUID;
            if (_assetDic.ContainsKey(path))
            {
                _assetDic.Remove(path);
            }
        }

        internal static void AddBundleInfo(BundleInfo bundleInfo)
        {
            string path = bundleInfo.InfoNameGUID;
            if (!_bundleDic.ContainsKey(path))
            {
                _bundleDic.Add(path, bundleInfo);
            }
        }

        internal static void RemoveBundleInfo(BundleInfo bundleInfo)
        {
            string path = bundleInfo.InfoNameGUID;
            if (_bundleDic.ContainsKey(path))
            {
                _bundleDic.Remove(path);
            }
        }

        internal static void AddSceneInfo(SceneInfo sceneInfo)
        {
            if (!_sceneDic.ContainsKey(sceneInfo.Scene.handle))
            {
                _sceneDic.Add(sceneInfo.Scene.handle, sceneInfo);
            }
        }

        internal static void RemoveSceneInfo(SceneInfo sceneInfo)
        {
            if (_sceneDic.ContainsKey(sceneInfo.Scene.handle))
            {
                _sceneDic.Remove(sceneInfo.Scene.handle);
            }
        }
    }

    internal enum NatAssetState
    {
        Waiting,  //未启动状态
        Inited,   //已处于实例化状态
        Release,  //处于释放状态
    }
}