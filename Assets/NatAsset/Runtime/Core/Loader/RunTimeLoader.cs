using System;
using System.Collections.Generic;
using UnityEngine;
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
                AssetInfo assetInfo = RuntimeData.GetAsset(targetPath, type);
                if (assetInfo == null)
                {
                    if (TaskSystem.TryGetProvider(targetPath, out BaseProvider baseProvider))
                    {
                        baseProvider.AddHandle(handle, priority, RunModel.Sync);
                    }
                    else
                    {
                        AssetProviderParam assetProviderParam = new AssetProviderParam(targetPath, type);
                        baseProvider = AssetProvider.Create<AssetProvider>(assetProviderParam, priority);
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
                AssetInfo assetInfo = RuntimeData.GetAsset(targetPath, type);
                if (assetInfo == null)
                {
                    if (TaskSystem.TryGetProvider(targetPath, out BaseProvider baseProvider))
                    {
                        baseProvider.AddHandle(handle, priority, RunModel.Async);
                    }
                    else
                    {
                        AssetProviderParam assetProviderParam = new AssetProviderParam(targetPath, type);
                        baseProvider = AssetProvider.Create<AssetProvider>(assetProviderParam, priority);
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

        public BatchAssetHandle LoadAssetsAsync(List<string> targetPaths, Type type, Priority priority)
        {
            BatchAssetHandle handle = null;
            if (targetPaths == null || targetPaths.Count == 0)
            {
                Debug.LogWarning("批量加载资源的资源名列表为空");
                handle = BatchAssetHandle.Create();
                return handle;
            }
            handle = BatchAssetHandle.Create();
            for (int i = 0; i < targetPaths.Count; i++)
            {
                AssetHandle assetHandle = LoadAssetAsync(targetPaths[i], type, priority);
                handle.AddAssetHandle(assetHandle);
            }
            handle.OnUpdate();
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