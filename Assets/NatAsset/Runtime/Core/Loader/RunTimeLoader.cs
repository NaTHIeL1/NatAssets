using System;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class RunTimeLoader : ILocalLoader
    {
        public AssetHandle LoadAsset(string targetPath, Type type, Priority priority)
        {
            AssetHandle handle = AssetHandle.Create(targetPath, type);
            if (targetPath == string.Empty)
            {
                handle.Error = $"资源路径不能为空，检查资源路径是否正确";
                handle.SetAsset(null);
            }
            else
            {
                AssetInfo assetInfo = RuntimeData.GetAsset(targetPath);
                if (assetInfo == null)
                {
                    if (TaskSystem.TryGetProvider(targetPath, out BaseProvider baseProvider))
                    {
                        baseProvider.AddHandle(handle, priority, RunModel.Sync);
                    }
                    else
                    {
                        baseProvider = AssetProvider.Create<AssetProvider>(targetPath, priority);
                        TaskSystem.AddProvider(baseProvider);
                        baseProvider.AddHandle(handle, priority, RunModel.Sync);
                    }
                }
                else
                {
                    assetInfo.AddRefCount();
                    handle.SetAsset(assetInfo.Asset);
                }
            }

            return handle;
        }

        public AssetHandle LoadAssetAsync(string targetPath, Type type, Priority priority)
        {
            AssetHandle handle = AssetHandle.Create(targetPath, type);
            if (targetPath == string.Empty)
            {
                handle.Error = $"资源路径不能为空，检查资源路径是否正确";
                handle.SetAsset(null);
            }
            else
            {
                AssetInfo assetInfo = RuntimeData.GetAsset(targetPath);
                if (assetInfo == null)
                {
                    if (TaskSystem.TryGetProvider(targetPath, out BaseProvider baseProvider))
                    {
                        baseProvider.AddHandle(handle, priority, RunModel.Async);
                    }
                    else
                    {
                        baseProvider = AssetProvider.Create<AssetProvider>(targetPath, priority);
                        TaskSystem.AddProvider(baseProvider);
                        baseProvider.AddHandle(handle, priority, RunModel.Async);
                    }
                }
                else
                {
                    assetInfo.AddRefCount();
                    handle.SetAsset(assetInfo.Asset);
                }
            }

            return handle;
        }

        // public SceneHandle LoadScene(string targetPath, LoadSceneMode loadSceneMode, Priority priority = Priority.Low)
        // {
        //     SceneHandle handle = SceneHandle.Create(targetPath, loadSceneMode);
        //     if (targetPath == string.Empty)
        //     {
        //         handle.Error = $"资源路径不能为空，检查资源路径是否正确";
        //         handle.SetScene(default);
        //     }
        //     else
        //     {
        //         SceneSystem.LoadScene(targetPath, loadSceneMode, priority, RunModel.Sync, handle);
        //     }
        //
        //     return handle;
        // }

        public SceneHandle LoadSceneAsync(string targetPath, LoadSceneMode loadSceneMode, Priority priority)
        {
            SceneHandle handle = SceneHandle.Create(targetPath, loadSceneMode);
            if (targetPath == string.Empty)
            {
                handle.Error = $"资源路径不能为空，检查资源路径是否正确";
                handle.SetScene(default);
            }
            else
            {
                SceneSystem.LoadScene(targetPath, loadSceneMode, priority,RunModel.Async,  handle);
            }

            return handle;
        }
    }
}