using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetMgr
    {
        /// <summary>
        /// 设置资源更新URL
        /// </summary>
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
        /// 异步批量加载，该接口只允许同类的资源加载，不同类型的请使用 NatAssetMgr.GetBatchAssetHandle()
        /// </summary>
        /// <param name="targetPaths"></param>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static BatchAssetHandle LoadAssetAsync(List<string> targetPaths, Type type, Priority priority = Priority.Low)
        {
            return CoreLoaderMgr.LoadAssetAsync(targetPaths, type, priority);
        }

        public static BatchAssetHandle GetBatchAssetHandle()
        {
            return CoreLoaderMgr.GetBatchAssetHandle();
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
        public static async void InstanceObjAsync(string targetPath, Action<GameObject> callback = null, Priority priority = Priority.Low)
        {
            AssetHandle handle = await LoadAssetAsync(targetPath, typeof(GameObject), priority);
            if (handle.IsSuccess)
            {
                GameObject obj = GameObject.Instantiate(handle.Asset as GameObject);
                handle.OnBind(obj);
                callback?.Invoke(obj);
            }
            else
            {
                Debug.LogErrorFormat("{0}资源加载失败", targetPath);
            }
        }

        /// <summary>
        /// 实例化方法自动绑定物体
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static async Task<GameObject> InstanceObjAsync(string targetPath, Priority priority = Priority.Low)
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
            sceneHandle.Dispose();
        }

        ///// <summary>
        ///// webRequest加载
        ///// </summary>
        ///// <param name="url"></param>
        ///// <param name="callback"></param>
        ///// <param name="priority"></param>
        ///// <param name="retryCount"></param>
        ///// <returns>返回该任务唯一GUID，用于取消加载</returns>
        //public static string SendWebRequest(string url, Action<bool, UnityWebRequest> callback,
        //    Priority priority = Priority.Low, int retryCount = -1)
        //{
        //    return CoreLoaderMgr.SendWebRequest(url, priority, callback, retryCount);
        //}

        ///// <summary>
        ///// 取消webrequest加载
        ///// </summary>
        ///// <param name="taskGuid"></param>
        //public static void DisposeWebRequest(string taskGuid)
        //{
        //    CoreLoaderMgr.DisposeWebRequest(taskGuid);
        //}

        /// <summary>
        /// 返回单位为byte/sencond
        /// </summary>
        /// <returns></returns>
        public static ulong GetTotalDownLoadRate()
        {
            return UpdaterSystem.TotalDownLoadRate();
        }

        /// <summary>
        /// 返回单位为byte/sencond
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static ulong GetGroupDownLoadRate(string groupName)
        {
            return UpdaterSystem.GroupDownLoadRate(groupName);
        }

        /// <summary>
        /// 开始校验可以更新的内容
        /// </summary>
        /// <param name="callback"></param>
        public static void CheckAssetVersion(Action<NatUpdaterInfo> callback)
        {
            UpdaterSystem.StartCheckAssetVersion(callback);
        }

        /// <summary>
        /// 停止校验
        /// </summary>
        public static void StopCheckAssetVersion()
        {
            UpdaterSystem.StopCheckAssetVersion();
        }

        // /// <summary>
        // /// 更新目标group资源组
        // /// </summary>
        // /// <param name="natUpdaterInfo"></param>
        // /// <param name="group"></param>
        // /// <returns></returns>
        // public static UpdateHandle UpdateGroup(NatUpdaterInfo natUpdaterInfo, string group)
        // {
        //     return UpdaterSystem.UpdateGroup(natUpdaterInfo, group);
        // }
        //
        // /// <summary>
        // /// 更新多个目标group资源组
        // /// </summary>
        // /// <param name="natUpdaterInfo"></param>
        // /// <param name="groups"></param>
        // /// <returns></returns>
        // public static UpdateHandles UpdateGroups(NatUpdaterInfo natUpdaterInfo, string[] groups)
        // {
        //     return UpdaterSystem.UpdateGroups(natUpdaterInfo, groups);
        // }
        
        /// <summary>
        /// 全量更新
        /// </summary>
        /// <param name="natUpdaterInfo"></param>
        /// <returns></returns>
        public static UpdateHandles UpdateAllGroup(NatUpdaterInfo natUpdaterInfo)
        {
            var updateHandles = UpdaterSystem.UpdateAllGroup(natUpdaterInfo);
            if (updateHandles != null)
            {
                UpdaterSystem.UpdateNatManifest(natUpdaterInfo, updateHandles);
            }

            return updateHandles;
        }
    }
}