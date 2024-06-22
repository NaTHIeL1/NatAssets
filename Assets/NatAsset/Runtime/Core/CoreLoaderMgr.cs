using System;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class CoreLoaderMgr
    {
        private static ILocalLoader _localLoader;

        internal static void Init(BaseManifestModeLoader modelLoader)
        {
            NatAssetSetting.InitData();
            SetLoader();
            TaskSystem.Init();
            SceneSystem.Init();
            RuntimeData.Init(modelLoader);
        }

        internal static void Update()
        {
            SceneSystem.Update();
            TaskSystem.Update();
        }

        internal static void Release()
        {
            SceneSystem.Release();
            TaskSystem.Release();
            RuntimeData.Release();
            _localLoader = null;
            NatAssetSetting.ReleaseData();
            ManifestLoad.Release();
            TypeStringGUIDPool.Release();
            ReferencePool.ReleaseAll();
            GC.Collect();
            GC.Collect();
        }

        internal static AssetHandle LoadAsset(string path, Type type, Priority priority)
        {
            CheckHasInit();
            return _localLoader.LoadAsset(path, type, priority);
        }

        internal static AssetHandle LoadAssetAsync(string path, Type type, Priority priority)
        {
            CheckHasInit();
            return _localLoader.LoadAssetAsync(path, type, priority);
        }

        internal static void UnLoadAsset(AssetHandle handle)
        {
            handle.Unload();
        }

        internal static SceneHandle LoadSceneAsync(string path, LoadSceneMode loadSceneMode, Priority priority)
        {
            CheckHasInit();
            return _localLoader.LoadSceneAsync(path, loadSceneMode, priority);
        }

        internal static string SendWebRequest(string url, Priority priority, Action<string, bool, UnityWebRequest> callback, int retryCount = -1)
        {
            WebRequestTask webRequestTask = WebRequestTask.Create(url, priority, callback, retryCount);
            TaskSystem.NetLoadTaskRunner.AddTask(webRequestTask);
            return webRequestTask.TaskGUID;
        }

        internal static void DisposeWebRequest(string taskGUID)
        {
            if (string.IsNullOrEmpty(taskGUID))
                return;
            TaskRunner taskRunner = TaskSystem.NetLoadTaskRunner;
            taskRunner.CancelTask(taskGUID);
        }

        private static void SetLoader()
        {
#if UNITY_EDITOR
            switch (NatAssetSetting.TRunWay)
            {
                case RunWay.Editor:
                    _localLoader = new EditorLoader();
                    break;
                case RunWay.PackageOnly:
                    _localLoader = new RunTimeLoader();
                    break;
            }
#else
            _localLoader = new RunTimeLoader();
#endif
        }

        private static bool CheckHasInit()
        {
            if (RuntimeData.NatAssetState == NatAssetState.Inited)
            {
                return true;
            }
            else
            {
                throw new Exception("等待配置文件初始化完成后再加载资源");
            }
        }
    }
}