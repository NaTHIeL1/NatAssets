using System;
using System.IO;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static partial class RuntimeData
    {
        /// <summary>
        /// 获取bundle路径，分为编辑器模式，运行时模式，webGL模式
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        internal static string GetRuntimeLoadPath(string bundleName)
        {
#if UNITY_EDITOR
            return Path.Combine(NatAssetSetting.EditorLoadPath, bundleName);
#endif
            switch (GetBundleLoadPath(bundleName))
            {
                case LoadPath.ReadOnly:
                    return Path.Combine(NatAssetSetting.ReadOnlyPath, bundleName);
                case LoadPath.ReadWrite:
                    return Path.Combine(NatAssetSetting.ReadWritePath, bundleName);
                default: return null;
            }
        }

        internal static void GetBundlePath(string path, out string bundleName, out string resName)
        {
            AnalysisPath(path, out bundleName, out resName, false);
        }

        internal static void GetBundleScenePath(string path, out string bundleName, out string sceneName)
        {
            AnalysisPath(path, out bundleName, out sceneName, true);
            GetSceneName(path, out sceneName);
        }

        internal static void GetSceneName(string path, out string sceneName)
        {
            SplitSceneName(path, out sceneName);
        }

        private static void AnalysisPath(string path, out string bundleName, out string resName, bool IsScene)
        {
            //if (AssetConfig.TryGetValue(path, out AssetManifest node))
            //{
            //    bundleName = node.BundleName;
            //    resName = node.AssetName;
            //    return;
            //}
            AssetManifest assetManifest = GetAssetManifest(path);
            if(assetManifest != null)
            {
                bundleName = assetManifest.BundleName;
                resName = assetManifest.AssetName;
                return;
            }

            throw new Exception(string.Format("资源路径:{0}，不存在于NatAssetConfig配置中", path));
        }

        private static void SplitSceneName(string source, out string sceneName)
        {
            char target = '/';
            char target1 = '.';
            int index = 0;
            int index1 = 0;
            int maxlength = source.Length - 1;
            char[] chars = source.ToCharArray();
            for (int i = maxlength; i > 0; i--)
            {
                if (target1 == chars[i])
                {
                    index1 = i;
                }

                if (chars[i] == target)
                {
                    index = i;
                    break;
                }
            }

            sceneName = source.Substring(index + 1, index1 - index - 1);
            if (index1 == 0) Debug.LogError("ScenenName 解析失败,路径不合法");
        }
    }
}