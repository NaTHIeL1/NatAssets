using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class UpdaterSystem
    {
        //下载速率
        private static ulong _downLoadRate = 0;
        //上一次记录的长度
        private static ulong _downLoadLastLength = 0;
        //总长度
        private static ulong _downLoadTotalLength = 0;
        private static float _recordTime = 0;
        internal static void Init()
        {

        }

        internal static void Update()
        {
            int num = TaskSystem.NetLoadTaskRunner.GetTaskNum();
            if (num > 0)
            {
                var taskGroup = TaskSystem.NetLoadTaskRunner.TaskGroups;
                if (taskGroup != null)
                {
                    for(int i =0;i< taskGroup.Count;i++)
                    {
                        var task = taskGroup[i];
                        ListDic<string, BaseTask> list = task.TaskDic;
                        for(int j=0;j< list.Count;j++)
                        {
                            BundleDownloadTask downTask = list.GetElementByIndex(j) as BundleDownloadTask;
                            if (downTask != null)
                            {
                                _downLoadTotalLength += downTask.DeltaRecordByteLength;
                            }
                        }
                    }
                }
                _recordTime += Time.deltaTime;
                if(_recordTime >= 1)
                {
                    _downLoadRate = _downLoadTotalLength - _downLoadLastLength;
                    _downLoadLastLength = _downLoadTotalLength;
                    _recordTime = 0;
                }
            }
            else if(num == 0)
            {
                _downLoadRate = 0;
                _downLoadLastLength = 0;
                _recordTime = 0;
                _downLoadTotalLength = 0;
            }
        }

        internal static void Release()
        {
            _downLoadRate = 0;
            _downLoadLastLength = 0;
            _recordTime = 0;
            _downLoadTotalLength = 0;
        }

        //检测资源更新
        internal static void StartCheckAssetVersion(Action<NatUpdaterInfo> callback)
        {
            VersionChecker.CheckVersion(callback);
        }

        internal static void StopCheckAssetVersion()
        {
            VersionChecker.Clear();
        }

        /// <summary>
        /// Handle对象唯一
        /// </summary>
        /// <param name="natUpdaterInfo"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        internal static UpdateHandle UpdateGroup(NatUpdaterInfo natUpdaterInfo, string group)
        {
            if (natUpdaterInfo == null || string.IsNullOrEmpty(group)) return null;
            if(!TaskSystem.TryGetNetProvider(group, out BaseProvider baseProvider))
            {
                GroupDownLoadProviderParam groupDownLoadProviderParam = 
                    new GroupDownLoadProviderParam(group, group, natUpdaterInfo);
                baseProvider = GroupDownLoadProvider.Create<GroupDownLoadProvider>(groupDownLoadProviderParam, Priority.Top);
                UpdateHandle updateHandle1 = UpdateHandle.Create(group);
                TaskSystem.AddNetProvider(baseProvider);
                baseProvider.AddHandle(updateHandle1, Priority.Top, RunModel.Async);
            }
            UpdateHandle updateHandle = baseProvider.Handles[0] as UpdateHandle;
            return updateHandle;
        }

        /// <summary>
        /// 检查更新后更新指定的资源
        /// </summary>
        /// <param name="natUpdaterInfo"></param>
        /// <param name="groups"></param>
        internal static UpdateHandle UpdateGroups(NatUpdaterInfo natUpdaterInfo, string[] groups)
        {
            if (groups == null) return null;
            //UpdateHandles updateHandles = UpdateHandles.Create()
            for (int i = 0; i < groups.Length; i++)
            {
                string group = groups[i];
                if (!TaskSystem.TryGetNetProvider(group, out BaseProvider baseProvider))
                {
                    GroupDownLoadProviderParam groupDownLoadProviderParam =
                        new GroupDownLoadProviderParam(group, group, natUpdaterInfo);
                    baseProvider = GroupDownLoadProvider.Create<GroupDownLoadProvider>(groupDownLoadProviderParam, Priority.Top);
                    UpdateHandles updateHandle1 = UpdateHandles.Create(group);
                    TaskSystem.AddNetProvider(baseProvider);
                    baseProvider.AddHandle(updateHandle1, Priority.Top, RunModel.Async);
                }
            }
            return null;
        }

        /// <summary>
        /// 返回单位为byte
        /// </summary>
        /// <returns></returns>
        internal static ulong TotalDownLoadRate()
        {
            return _downLoadRate;
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