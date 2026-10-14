using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class BundleInfo : BaseInfo
    {
        internal AssetBundle Bundle;
        private static long _sequence;

        internal static BundleInfo CreateBundleInfo(string path, AssetBundle bundle)
        {
            BundleInfo info = Create<BundleInfo>(path);
            info.Bundle = bundle;
            return info;
        }

        protected override void OnAddRefCount() { CancelUnload(); RuntimeData.MarkLifetimeDirty(); }
        protected override void OnRedRefCount() => RuntimeData.MarkLifetimeDirty();
        protected override void OnLock() { CancelUnload(); RuntimeData.MarkLifetimeDirty(); }
        protected override void OnUnLock() => RuntimeData.MarkLifetimeDirty();

        internal void ScheduleUnload()
        {
            if (_unLoadTask != null || RuntimeData.IsReleasing) return;
            var parameter = new ComTaskParam("bundle-unload:" + ++_sequence, InfoNameGUID);
            _unLoadTask = BaseTask.CreateTask<BundleUnLoadTask>(parameter, Priority.Low);
            TaskSystem.UnLoadTaskRunner.AddTask(_unLoadTask);
        }

        internal bool IsUnloadTask(BaseTask task) => ReferenceEquals(_unLoadTask, task);
        internal void FinishUnloadTask(BaseTask task) { if (IsUnloadTask(task)) _unLoadTask = null; }
        internal void CancelUnload() { _unLoadTask?.CancelTask(); _unLoadTask = null; }

        protected override void OnClear()
        {
            _unLoadTask = null;
            Bundle = null;
        }
    }
}