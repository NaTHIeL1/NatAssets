using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class BundleUnLoadTask : BaseTask
    {
        private float _elapsed;
        private BundleInfo _target;
        private long _generation;
        protected override void OnCreate()
        {
            _elapsed = 0;
            _target = RuntimeData.GetBundle(TaskName);
            _generation = _target?.Generation ?? 0;
        }
        public override float Progress
        {
            get => NatAssetSetting.BundleDelayTime <= 0 ? 1 : Mathf.Clamp01(_elapsed / NatAssetSetting.BundleDelayTime);
            protected set { }
        }
        internal override void TaskUpdate()
        {
            if (IsDone) return;
            SetTaskState(TaskState.Running);
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= NatAssetSetting.BundleDelayTime) Execute();
        }
        private void Execute()
        {
            RuntimeData.TryUnloadBundle(_target, _generation, this);
            SetTaskState(TaskState.End);
        }
        protected override void OnCancelTask() => SetTaskState(TaskState.End);
        protected override void OnSwitchToSync() { if (!IsDone) Execute(); }
        protected override void OnClear() { _target = null; _generation = 0; _elapsed = 0; }
    }
}