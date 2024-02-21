using System;
using System.Collections.Generic;
using System.Linq;
using NATFrameWork.NatAsset.Runtime;

#if UNITY_EDITOR
namespace NATFrameWork.Profiler
{
    [Obsolete("供编辑器使用")]
    public static class ProfilerInfo
    {
        private static Dictionary<string, HandleInfo> _handleInfos = new Dictionary<string, HandleInfo>();
        private static List<RefInfo> _refInfos = new List<RefInfo>();
        private static List<LoadTaskInfo> _loadTaskInfos = new List<LoadTaskInfo>();
        private static List<UnLoadTaskInfo> _unLoadTaskInfos = new List<UnLoadTaskInfo>();

        public static Dictionary<string, HandleInfo> GetHandleInfos()
        {
            return _handleInfos;
        }

        internal static void AddHandleInfo(IHandle handle)
        {
            HandleInfo handleInfo = new HandleInfo();
            NatAssetType natAssetType = NatAssetType.Asset;
            if (handle is AssetHandle)
            {
                natAssetType = NatAssetType.Asset;
            }
            else if (handle is SceneHandle)
            {
                natAssetType = NatAssetType.Scene;
            }

            handleInfo.SetParam(handle.Name, string.Empty, natAssetType);
            _handleInfos.Add(handle.Name, handleInfo);
        }

        internal static void RemoveHandleInfo(IHandle handle)
        {
            if (_handleInfos.ContainsKey(handle.Name))
            {
                _handleInfos.Remove(handle.Name);
            }
        }

        public static List<RefInfo> GetRefInfos()
        {
            _refInfos.Clear();
            Dictionary<string, AssetInfo> assetInfos = RuntimeData.AssetDic;
            for (int i = 0; i < assetInfos.Count; i++)
            {
                var tempInfo = assetInfos.ElementAt(i);
                RefInfo tempRefInfo = new RefInfo();
                AssetInfo assetInfo = tempInfo.Value;
                tempRefInfo.SetInfo(NatAssetType.Asset, assetInfo.InfoNameGUID, assetInfo.RefCount);
                _refInfos.Add(tempRefInfo);
            }

            Dictionary<int, SceneInfo> sceneInfos = RuntimeData.SceneDic;
            for (int i = 0; i < sceneInfos.Count; i++)
            {
                var tempInfo = sceneInfos.ElementAt(i);
                RefInfo tempRefInfo = new RefInfo();
                SceneInfo sceneInfo = tempInfo.Value;
                tempRefInfo.SetInfo(NatAssetType.Scene, sceneInfo.InfoNameGUID, sceneInfo.RefCount);
                _refInfos.Add(tempRefInfo);
            }

            Dictionary<string, BundleInfo> bundleInfos = RuntimeData.BundleDic;
            for (int i = 0; i < bundleInfos.Count; i++)
            {
                var tempInfo = bundleInfos.ElementAt(i);
                RefInfo tempRefInfo = new RefInfo();
                BundleInfo bundleInfo = tempInfo.Value;
                tempRefInfo.SetInfo(NatAssetType.Bundle, bundleInfo.InfoNameGUID, bundleInfo.RefCount);
                _refInfos.Add(tempRefInfo);
            }

            return _refInfos;
        }

        public static List<LoadTaskInfo> GetLoadTaskInfos()
        {
            _loadTaskInfos.Clear();
            TaskRunner taskRunner = TaskSystem.LoadTaskRunner;
            for (int i = taskRunner.Count - 1; i >= 0; i--)
            {
                TaskGroup tempGroup = taskRunner.TaskGroups[i];
                AddLoadTaskInfo(tempGroup.TaskDic);
            }

            TaskRunner netTaskRunner = TaskSystem.NetLoadTaskRunner;
            for (int i = netTaskRunner.Count - 1; i >= 0; i--)
            {
                TaskGroup tempGroup = netTaskRunner.TaskGroups[i];
                AddLoadTaskInfo(tempGroup.TaskDic);
            }

            return _loadTaskInfos;
        }

        private static void AddLoadTaskInfo(ListDic<string, BaseTask> taskGroup)
        {
            for (int i = 0; i < taskGroup.Count; i++)
            {
                BaseTask baseTask = taskGroup.GetElementByIndex(i);
                LoadTaskInfo loadTaskInfo = new LoadTaskInfo();
                loadTaskInfo.SetParam(baseTask);
                _loadTaskInfos.Add(loadTaskInfo);
            }
        }

        public static List<UnLoadTaskInfo> GetUnLoadTaskInfos()
        {
            _unLoadTaskInfos.Clear();
            TaskRunner taskRunner = TaskSystem.UnLoadTaskRunner;
            for (int i = taskRunner.Count - 1; i >= 0; i--)
            {
                TaskGroup tempGroup = taskRunner.TaskGroups[i];
                AddUnLoadTaskInfo(tempGroup.TaskDic);
            }

            return _unLoadTaskInfos;
        }

        private static void AddUnLoadTaskInfo(ListDic<string, BaseTask> taskGroup)
        {
            for (int i = 0; i < taskGroup.Count; i++)
            {
                BaseTask baseTask = taskGroup.GetElementByIndex(i);
                UnLoadTaskInfo unLoadTaskInfo = new UnLoadTaskInfo();
                unLoadTaskInfo.SetParam(baseTask);
                _unLoadTaskInfos.Add(unLoadTaskInfo);
            }
        }
    }
}
#endif