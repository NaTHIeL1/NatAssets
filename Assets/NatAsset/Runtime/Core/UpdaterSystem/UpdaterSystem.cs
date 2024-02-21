using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class UpdaterSystem
    {
        internal static void CheckAssetVersion()
        {
            
        }

        /// <summary>
        /// 返回单位为byte
        /// </summary>
        /// <returns></returns>
        internal static ulong TotalDownLoadRate()
        {
            //todo:计算全部执行中的DownLoadTask的速率
            return 0;
        }

        internal static ulong GroupDownLoadRate(string groupName)
        {
            //todo:计算当前Group的速率
            return 0;
        }

        internal static void UpdateBundleByGroup(string group, Priority priority = Priority.Middle)
        {
            
        }

        internal static void UpdateBundleByBundleMainfest(string group, BundleManifest bundleManifest, Action<bool> callback, Priority priority = Priority.High)
        {

        }

        internal static void StopGroupUpdate(string group)
        {

        }
    }
}