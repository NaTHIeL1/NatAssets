using System;
using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetUtil
    {
        //初始化配置
        public static void Init(BaseManifestModeLoader modelLoader, Action callback, Action<string> onFailure = null)
        {
            RuntimeData.CompleteCallBack += callback;
            RuntimeData.FailureCallBack += onFailure;
            CoreLoaderMgr.Init(modelLoader);
        }

        //执行任务加载器循环
        public static void Update()
        {
            CoreLoaderMgr.Update();
        }

        public static void Release()
        {
            CoreLoaderMgr.Release();
        }

        /// <summary>等待进行中的加载、场景卸载和资源清理完成；重启后再初始化。</summary>
        public static System.Threading.Tasks.Task ReleaseAsync()
        {
            return CoreLoaderMgr.Release();
        }

        /// <summary>
        /// 立即卸载所有未使用的资源
        /// </summary>
        public static void ImmediateUnLoadUnUseAsset()
        {
            TaskSystem.FreeUnUseAsset();
        }
        
        /// <summary>
        /// 获取全部bundle的MD5信息
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string,string> GetAllBundleMD5()
        {
            return RuntimeData.GetAllBundleMD5();
        }
        
        /// <summary>
        /// 获取指定bundle的MD5信息
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public static string GetBundleMD5(string bundleName)
        {
            return RuntimeData.GetBundleMD5(bundleName);
        }

        public static Hash128 GetBundleHash(string bundleName)
        {
            return RuntimeData.GetAssetBundleHash(bundleName);
        }

        /// <summary>
        /// 检查是否拥有该bundle
        /// </summary>
        /// <param name="bundlePath"></param>
        /// <returns></returns>
        public static bool CheckHasBundle(string bundlePath)
        {
            return RuntimeData.CheckHasBundle(bundlePath);
        }

        public static List<string> GetBundleAllAssetPath(string bundlePath)
        {
            return RuntimeData.GetBundleAllAssetsPath(bundlePath);
        }
    }
}
