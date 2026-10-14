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
        private static Dictionary<string, AssetInfo> _assetDic = new Dictionary<string, AssetInfo>(1000);
        //Bundle字典
        private static Dictionary<string, BundleInfo> _bundleDic = new Dictionary<string, BundleInfo>(1000);

        //场景字典
        private static Dictionary<int, SceneInfo> _sceneDic = new Dictionary<int, SceneInfo>(5);

        internal static event Action CompleteCallBack;
        internal static event Action<string> FailureCallBack;

        private static NatAssetState _natAssetState = NatAssetState.Waiting;

        internal static NatAssetState NatAssetState
        {
            get { return _natAssetState; }
        }

        internal static void SetNatAssetState(NatAssetState natAssetState)
        {
            _natAssetState = natAssetState;
        }

        private static bool CanInit => NatAssetState != NatAssetState.Release;

        internal static void Init(BaseManifestModeLoader modelLoader)
        {
            IsReleasing = false;
            _manifestLoader = modelLoader;
            SetNatAssetState(NatAssetState.Waiting);
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
                else if (manifestModeState == ManifestModeState.Error)
                {
                    SetNatAssetState(NatAssetState.Failed);
                    var failure = FailureCallBack;
                    CompleteCallBack = null;
                    FailureCallBack = null;
                    string error = _manifestLoader.LastError ?? "NatAsset 清单初始化失败。";
                    UnityEngine.Debug.LogError(error);
                    CompletionCallbacks.Invoke(failure, error, "初始化失败");
                }
            }
            _manifestLoader.LoadCallBack = Callback;
            _manifestLoader.StartLoader();
        }
        
        private static void ExeCompleteCallbak()
        {
            if (!CanInit)
            {
                if (NatAssetState == NatAssetState.Release)
                    CompleteCallBack = null;
                return;
            }

            SetNatAssetState(NatAssetState.Inited);
            var callback = CompleteCallBack;
            CompleteCallBack = null;
            FailureCallBack = null;
            CompletionCallbacks.Invoke(callback, "初始化完成");
        }

        internal static void Release()
        {
            CompleteCallBack = null;
            FailureCallBack = null;
            _manifestLoader?.StopLoader();
            SetNatAssetState(NatAssetState.Release);
            foreach (SceneInfo scene in _sceneDic.Values) BaseInfo.Release(scene);
            _sceneDic.Clear();
            ClearLifetimes();
            ReleaseOwnedBundles();
            _assetDic.Clear();
            //_assetGroupDic.Clear();
            _bundleDic.Clear();
            //UnLoadMainfest();
        }

        /// <summary>
        /// 根据类型和路径获取资源数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static AssetInfo GetAsset(string path, Type type)
        {
            string guid = TypeStringGUIDPool.GetTargetGUID(type, NormalizeAssetPath(path));
            if (_assetDic.TryGetValue(guid, out AssetInfo assetInfo))
            {
                return assetInfo;
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
            string guid = TypeStringGUIDPool.GetTargetGUID(assetInfo);
            if (!_assetDic.ContainsKey(guid))
            {
                _assetDic.Add(guid, assetInfo);
                RegisterAssetLifetime(assetInfo);
            }
        }

        internal static void RemoveAssetInfo(AssetInfo assetInfo)
        {
            string guid = TypeStringGUIDPool.GetTargetGUID(assetInfo);
            if (_assetDic.ContainsKey(guid))
            {
                _assetDic.Remove(guid);
            }
        }

        internal static void AddBundleInfo(BundleInfo bundleInfo)
        {
            string path = bundleInfo.InfoNameGUID;
            if (!_bundleDic.ContainsKey(path))
            {
                _bundleDic.Add(path, bundleInfo);
                MarkLifetimeDirty();
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
        Failed,   //初始化失败，禁止继续加载
        Release,  //处于释放状态
    }
}
