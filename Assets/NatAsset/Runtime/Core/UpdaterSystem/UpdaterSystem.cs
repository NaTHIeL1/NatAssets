using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class UpdaterSystem
    {
        private static string _id = string.Empty;
        private static Action<NatUpdaterInfo> _ListStringCallBack;

        internal static void Init()
        {

        }

        internal static void Update()
        {
        }

        internal static void Release()
        {

        }

        //检测资源更新
        internal static void StartCheckAssetVersion(Action<NatUpdaterInfo> callback)
        {
            _ListStringCallBack = callback;
            string remotePath = Path.Combine(NatAssetSetting.AssetServerURL, NatAssetSetting.ConfigName);
            //Action<bool, UnityWebRequest> cdnManifestCallback = LoadCDNManifestCallBack;
            //_id = CoreLoaderMgr.SendWebRequest(remotePath, Priority.Top, cdnManifestCallback);
        }

        internal static void StopCheckAssetVersion()
        {
            _ListStringCallBack = null;
            CoreLoaderMgr.DisposeWebRequest( _id );
            _id = string.Empty;
        }

        private static void LoadCDNManifestCallBack(string taskId, bool success, UnityWebRequest webRequest)
        {
            List<string> strings = new List<string>();
            if (success)
            {
                
                NatAssetManifest cdnManifest = NatAssetManifest.DeserializeFromBinary(webRequest.downloadHandler.data);
                if (cdnManifest != null)
                {
                    Dictionary<string, BundleManifest> cndBundleManifest = cdnManifest.BundleManifestDic;

                }
            }
            //_ListStringCallBack?.Invoke(strings);
            StopCheckAssetVersion();
        }

        internal static void UpdateGroup(string grooup)
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