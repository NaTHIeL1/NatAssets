using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class BundleUnLoadTask : BaseTask
    {
        private float _executionTime = 0;

        protected override void OnCreate()
        {
            _executionTime = 0;
        }

        public override float Progress
        {
            get
            {
                return _executionTime / NatAssetSetting.AssetDelayTime;
            }
            protected set => base.Progress = value;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                SetTaskState(TaskState.Running);
            }

            if (TaskState == TaskState.Running)
            {
                _executionTime += Time.deltaTime;
                if (_executionTime >= NatAssetSetting.BundleDelayTime)
                {
                    UnLoad();
                    SetTaskState(TaskState.End);
                }
            }

            if (TaskState == TaskState.End)
                return;
        }

        protected override void OnCancelTask()
        {
            if (TaskState == TaskState.Waiting)
            {
                SetTaskState(TaskState.End);
                return;
            }

            if (TaskState == TaskState.Running)
            {
                _executionTime = 0;
                Progress = 0;
                SetTaskState(TaskState.End);
                return;
            }

            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        protected override void OnSwitchToSync()
        {
            if (TaskState != TaskState.End)
            {
                Progress = 1;
                UnLoad();
                SetTaskState(TaskState.End);
            }
        }

        protected override void OnClear()
        {
            _executionTime = 0;
        }

        private void UnLoad()
        {
            BundleInfo bundleInfo = RuntimeData.GetBundle(TaskName);
            RuntimeData.RemoveBundleInfo(bundleInfo);
            bundleInfo.Bundle.Unload(true);
            bundleInfo.Bundle = null;
            BundleInfo.Release(bundleInfo);
        }
    }
}