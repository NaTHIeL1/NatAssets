using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    //统一处理场景加载相关逻辑
    internal static class SceneSystem
    {
        private static string _tempSceneName = "NatAssetTempScene";

        //场景句柄和异步句柄不在销毁框架时销毁
        private static List<SceneHandle> _sceneHandles = new List<SceneHandle>();

        private static List<UnLoadSceneOperation> _unLoadSceneOperations = new List<UnLoadSceneOperation>();

        //记录属于BuildIn的场景
        private static List<string> _buildInScene = new List<string>();
        private static SceneHandle _mainSceneHandle;
        private static List<CacheAdditonScene> _waitLoadHandlesQueue = new List<CacheAdditonScene>();


        internal static void Init()
        {
            int buildInCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < buildInCount; i++)
            {
                Scene tempScene = SceneManager.GetSceneByBuildIndex(i);
                _buildInScene.Add(tempScene.path);
            }

            //获取启动时的场景并作为当前使用的场景，模拟一个场景句柄
            Scene scene = SceneManager.GetActiveScene();
            string defaultSceneGUID = $"{scene.path}-{TaskRandom.GetTaskRandom()}";
            SceneInfo sceneInfo = SceneInfo.CreateSceneInfo(defaultSceneGUID, scene, null);
            RuntimeData.AddSceneInfo(sceneInfo);
            sceneInfo.AddRefCount();

            SceneHandle defaultSceneHandle = SceneHandle.Create(scene.path, LoadSceneMode.Single);
            defaultSceneHandle.SetScene(scene);
            _sceneHandles.Add(defaultSceneHandle);

            //设置为主场景句柄
            _mainSceneHandle = defaultSceneHandle;
        }

        internal static void Update()
        {
            //轮询异步句柄
            for (int i = 0; i < _unLoadSceneOperations.Count; i++)
            {
                UnLoadSceneOperation unLoadSceneOperation = _unLoadSceneOperations[i];
                unLoadSceneOperation.Update();
                if (unLoadSceneOperation.isDone)
                {
                    UnLoadSceneOperation.Release(unLoadSceneOperation);
                    _unLoadSceneOperations.Remove(unLoadSceneOperation);
                    i--;
                }
            }
        }

        internal static void Release()
        {
            _buildInScene.Clear();
        }

        internal static void LoadScene(string targetPath, LoadSceneMode loadSceneMode, Priority priority,
            RunModel runModel, SceneHandle sceneHandle)
        {
            if (loadSceneMode == LoadSceneMode.Single)
            {
                _mainSceneHandle.OnLoaded -= LoadAllSubSceneOnMainSceneComplete;
                //清空缓存的未加载附属场景
                ReleaseCacheSubSceneHandle();
                UnLoadAllScene();
                _mainSceneHandle = sceneHandle;
                sceneHandle.OnLoaded += LoadAllSubSceneOnMainSceneComplete;
            }
            else
            {
                if (!_mainSceneHandle.IsDone)
                {
                    //主场景未加载完缓存附属场景
                    CacheSubSceneHandle(targetPath, loadSceneMode, priority, runModel, sceneHandle);
                    return;
                }
            }

            string providerName = $"{targetPath}-{TaskRandom.GetTaskRandom()}";
            BaseProvider sceneProvider = null;
#if UNITY_EDITOR
            if (NatAssetSetting.TRunWay == RunWay.Editor)
            {
                SceneProviderParam sceneProviderParam = new SceneProviderParam(providerName, targetPath, loadSceneMode);
                sceneProvider = EditorSceneProvider.Create<EditorSceneProvider>(sceneProviderParam, priority);
            }
            else
            {
                sceneProvider = CreateRunTimeProvider(targetPath, providerName, priority, loadSceneMode);
            }
#else
            sceneProvider = CreateRunTimeProvider(targetPath, providerName, priority, loadSceneMode);
#endif

            _sceneHandles.Add(sceneHandle);
            TaskSystem.AddProvider(sceneProvider);

            sceneProvider.AddHandle(sceneHandle, priority, runModel);
        }

        internal static void UnLoadAllScene()
        {
            //立即取消全部的场景任务加载逻辑，并将句柄置为无效句柄
            //已加载的场景完毕的场景等待被动卸载
            for (int i = 0; i < _sceneHandles.Count; i++)
            {
                SceneHandle handle = _sceneHandles[i];
                handle.ReleaseLogic();
            }

            _sceneHandles.Clear();
        }

        internal static bool IsBuildInScene(string scenePath)
        {
            return _buildInScene.Contains(scenePath);
        }

        internal static List<SceneHandle> GetCurrentScenes(List<SceneHandle> source = null)
        {
            if (source == null)
                source = new List<SceneHandle>();
            for (int i = 0; i < _sceneHandles.Count; i++)
            {
                source.Add(_sceneHandles[i]);
            }

            return source;
        }

        internal static void AddOperation(UnLoadSceneOperation unLoadSceneOperation)
        {
            _unLoadSceneOperations.Add(unLoadSceneOperation);
        }

        /// <summary>
        /// 创建运行时句柄
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="providerName"></param>
        /// <param name="priority"></param>
        /// <param name="loadSceneMode"></param>
        /// <returns></returns>
        private static BaseProvider CreateRunTimeProvider(string targetPath, string providerName, Priority priority,
            LoadSceneMode loadSceneMode)
        {
            BaseProvider sceneProvider = null;
            SceneProviderParam sceneProviderParam = new SceneProviderParam(providerName, targetPath, loadSceneMode);
            if (IsBuildInScene(targetPath))
            {
                //走内置场景加载逻辑
                sceneProvider = SceneBuildInProvider.Create<SceneBuildInProvider>(sceneProviderParam, priority);
            }
            else
            {
                sceneProvider = SceneBundleProvider.Create<SceneBundleProvider>(sceneProviderParam, priority);
            }

            return sceneProvider;
        }

        /// <summary>
        /// 主场景加载完毕后就启动缓存子场景加载
        /// </summary>
        private static void LoadAllSubSceneOnMainSceneComplete(SceneHandle sceneHandle)
        {
            if (sceneHandle.IsSuccess)
            {
                _mainSceneHandle = sceneHandle;
                for (int i = 0; i < _waitLoadHandlesQueue.Count; i++)
                {
                    CacheAdditonScene cacheAdditonScene = _waitLoadHandlesQueue[i];
                    LoadScene(cacheAdditonScene.targetPath, cacheAdditonScene.loadSceneMode, cacheAdditonScene.priority,
                        cacheAdditonScene.runModel, cacheAdditonScene.sceneHandle);
                }
            }
            ReleaseCacheSubSceneHandle();
        }

        /// <summary>
        /// 在主场景未加载完时缓存子场景句柄
        /// </summary>
        private static void CacheSubSceneHandle(string targetPath, LoadSceneMode loadSceneMode, Priority priority,
            RunModel runModel, SceneHandle sceneHandle)
        {
            CacheAdditonScene cacheAdditonScene = new CacheAdditonScene()
            {
                targetPath = targetPath,
                loadSceneMode = loadSceneMode,
                priority = priority,
                runModel = runModel,
                sceneHandle = sceneHandle,
            };
            _waitLoadHandlesQueue.Add(cacheAdditonScene);
        }

        private static void ReleaseCacheSubSceneHandle()
        {
            //对应未加载句柄都置为无效句柄
            for (int i = 0; i < _waitLoadHandlesQueue.Count; i++)
            {
                CacheAdditonScene additonScene = _waitLoadHandlesQueue[i];
                additonScene.sceneHandle.SetToInValid();
            }
            _waitLoadHandlesQueue.Clear();
        }

        internal struct CacheAdditonScene
        {
            internal string targetPath;
            internal LoadSceneMode loadSceneMode;
            internal Priority priority;
            internal RunModel runModel;
            internal SceneHandle sceneHandle;
        }
    }
}