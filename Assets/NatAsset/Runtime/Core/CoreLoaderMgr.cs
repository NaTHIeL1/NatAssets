using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class CoreLoaderMgr
    {
        private static ILocalLoader _localLoader;
        private static System.Threading.Tasks.Task _releaseTask;

        internal static void Init(BaseManifestModeLoader modelLoader)
        {
            if (_releaseTask != null && !_releaseTask.IsCompleted)
                throw new InvalidOperationException("NatAsset 上次退出尚未完成。");
            NatAssetSetting.InitData();
            SetLoader();
            OperationSystem.Init();
            TaskSystem.Init();
            SceneSystem.Init();
            UpdaterSystem.Init();
            RuntimeData.Init(modelLoader);
        }

        internal static void Update()
        {
            if (RuntimeData.IsReleasing || RuntimeData.NatAssetState == NatAssetState.Failed) return;
            SceneSystem.Update();
            TaskSystem.Update();
            UpdaterSystem.Update();
            OperationSystem.Update();
        }

        internal static System.Threading.Tasks.Task Release()
        {
            if (RuntimeData.IsReleasing) return _releaseTask ?? System.Threading.Tasks.Task.CompletedTask;
            RuntimeData.BeginRelease();
            _releaseTask = ReleaseAsync();
            return _releaseTask;
        }

        private static async System.Threading.Tasks.Task ReleaseAsync()
        {
            try
            {
                UpdaterSystem.Release();
                await TaskSystem.DrainProvidersAsync();
                await RuntimeData.UnloadManagedScenesAsync();
                SceneSystem.Release();
                TaskSystem.Release();
                OperationSystem.Release();
                RuntimeData.Release();
                _localLoader = null;
                NatAssetSetting.ReleaseData();
                ManifestLoad.Release();
                TypeStringGUIDPool.Release();
                ReferencePool.ReleaseAll();
                var cleanup = UnityEngine.Resources.UnloadUnusedAssets();
                while (!cleanup.isDone) await System.Threading.Tasks.Task.Yield();
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogException(error);
                throw;
            }
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

        internal static BatchAssetHandle LoadAssetAsync(List<string> paths, Type type, Priority priority)
        {
            CheckHasInit();
            return _localLoader.LoadAssetsAsync(paths, type, priority);
        }
        internal static void UnLoadAsset(AssetHandle handle)
        {
            handle.Dispose();
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

        internal static BatchAssetHandle GetBatchAssetHandle()
        {
            return BatchAssetHandle.Create();
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
            if (RuntimeData.IsReleasing) throw new InvalidOperationException("NatAsset 正在退出，不能加载新资源。");
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
