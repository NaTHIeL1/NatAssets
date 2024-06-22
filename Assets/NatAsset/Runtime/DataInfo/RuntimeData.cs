using System;
using System.Collections.Generic;
using UnityEngine;
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
        private static Dictionary<Type, AssetTypeGroup> _assetGroupDic = new Dictionary<Type, AssetTypeGroup>(1000);
        //Bundle字典
        private static Dictionary<string, BundleInfo> _bundleDic = new Dictionary<string, BundleInfo>(1000);

        //场景字典
        private static Dictionary<int, SceneInfo> _sceneDic = new Dictionary<int, SceneInfo>(5);

        internal static event Action CompleteCallBack;

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
            CompleteCallBack?.Invoke();
            CompleteCallBack = null;
        }

        internal static void Release()
        {
            _manifestLoader.StopLoader();
            SetNatAssetState(NatAssetState.Release);
            _assetDic.Clear();
            _assetGroupDic.Clear();
            _sceneDic.Clear();
            _bundleDic.Clear();
            //UnLoadMainfest();
        }

        //对回收的资源进行标记
        internal static void MarkAndLaunchUnLoad()
        {
            foreach (KeyValuePair<string, AssetInfo> keyValuePair in _assetDic)
                keyValuePair.Value.CollectionMark();

            foreach(KeyValuePair<Type,AssetTypeGroup> keyValuePair in _assetGroupDic)
                keyValuePair.Value.CollectionMark();
            foreach (KeyValuePair<int, SceneInfo> keyValuePair in _sceneDic)
                keyValuePair.Value.CollectionMark();

            foreach (KeyValuePair<string, AssetInfo> keyValuePair in _assetDic)
                keyValuePair.Value.RedRefCount();
            foreach (KeyValuePair<int, SceneInfo> keyValuePair in _sceneDic)
                keyValuePair.Value.RedRefCount();
        }

        /// <summary>
        /// 根据类型和路径获取资源数据
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static AssetInfo GetAsset(string path, Type type)
        {
            if(_assetGroupDic.TryGetValue(type, out AssetTypeGroup group))
            {
                return group.GetAssetInfo(path);
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
            Type type = assetInfo.AssetType;
            AssetTypeGroup group;
            if (_assetGroupDic.TryGetValue(type, out group))
            {
                group.AddAssetInfo(assetInfo);
            }
            else
            {
                group = new AssetTypeGroup();
                _assetGroupDic.Add(type, group);
                group.AddAssetInfo(assetInfo);
            }
        }

        internal static void RemoveAssetInfo(AssetInfo assetInfo)
        {
            Type type = assetInfo.AssetType;
            AssetTypeGroup group;
            if(_assetGroupDic.TryGetValue(type,out group))
            {
                group.RemoveAssetInfo(assetInfo);
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