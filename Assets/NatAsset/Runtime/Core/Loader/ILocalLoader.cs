using System;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public interface ILocalLoader
    {
        //资源加载
        AssetHandle LoadAsset(string targetPath, Type type, Priority priority);

        AssetHandle LoadAssetAsync(string targetPath, Type type, Priority priority);

        //SceneHandle LoadScene(string targetPath, LoadSceneMode loadSceneMode, Priority priority = Priority.Low);
        SceneHandle LoadSceneAsync(string targetPath, LoadSceneMode loadSceneMode, Priority priority);
        //AsyncOperation UnloadSceneAsync(SceneHandle sceneHandle);
    }
}