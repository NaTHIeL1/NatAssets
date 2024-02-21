using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetMgr
    {
        public static string UpdateURLAddress 
        {
            get => NatAssetSetting.AssetServerURL;
            set => NatAssetSetting.AssetServerURL = value;
        }
        /// <summary>
        /// 同步资源加载
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AssetHandle LoadAsset(string targetPath, Type type, Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadAsset(targetPath, type, priority);
        }

        /// <summary>
        /// 同步资源加载
        /// </summary>
        /// <param name="targetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AssetHandle LoadAsset<T>(string targetPath, Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadAsset(targetPath, typeof(T), priority);
        }

        /// <summary>
        /// 异步资源加载
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AssetHandle LoadAssetAsync(string targetPath, Type type, Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadAssetAsync(targetPath, type, priority);
        }

        /// <summary>
        /// 异步资源加载
        /// </summary>
        /// <param name="targetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static AssetHandle LoadAssetAsync<T>(string targetPath, Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadAssetAsync(targetPath, typeof(T), priority);
        }

        /// <summary>
        /// 实例化方法自动绑定物体
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static GameObject InstanceObj(string targetPath, Priority priority = Priority.Low)
        {
            AssetHandle handle = LoadAsset(targetPath, typeof(GameObject), priority);
            if (handle.IsSuccess)
            {
                GameObject obj = GameObject.Instantiate(handle.Asset as GameObject);
                handle.OnBind(obj);
                return obj;
            }
            else
            {
                Debug.LogErrorFormat("{0}资源加载失败", targetPath);
                return null;
            }
        }

        /// <summary>
        /// 实例化方法自动绑定物体
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static async UniTask<GameObject> InstanceObjAsync(string targetPath, Priority priority = Priority.Low)
        {
            AssetHandle handle = await LoadAssetAsync(targetPath, typeof(GameObject), priority);
            if (handle.IsSuccess)
            {
                GameObject obj = GameObject.Instantiate(handle.Asset as GameObject);
                handle.OnBind(obj);
                return obj;
            }
            else
            {
                Debug.LogErrorFormat("{0}资源加载失败", targetPath);
                return null;
            }
        }

        /// <summary>
        /// 手动卸载释放句柄
        /// </summary>
        /// <param name="handle"></param>
        public static void UnLoadAsset(AssetHandle handle)
        {
            CoreLoaderMgr.UnLoadAsset(handle);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="loadSceneMode"></param>
        /// <returns></returns>
        public static SceneHandle LoadSceneAsync(string scenePath, LoadSceneMode loadSceneMode,
            Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadSceneAsync(scenePath, loadSceneMode, priority);
        }

        /// <summary>
        /// 手动卸载场景
        /// </summary>
        /// <param name="scenePath"></param>
        /// <param name="unloadSceneOptions"></param>
        /// <returns></returns>
        public static void UnLoadSceneAsync(SceneHandle sceneHandle)
        {
            sceneHandle.Unload();
        }

        public static void SendWebRequest(string url, Action<bool, UnityWebRequest> callback,
            Priority priority = Priority.Low, int retryCount = -1)
        {
            CoreLoaderMgr.SendWebRequest(url, priority, callback, retryCount);
        }
    }
}