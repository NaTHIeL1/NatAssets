using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class AssetUnLoadTask : BaseTask
    {
        private float _elapsed;
        private AssetLifetimeInfo _target;
        private long _generation;
        protected override void OnCreate()
        {
            _elapsed = 0;
            _target = RuntimeData.GetLifetime(TaskName);
            _generation = _target?.Generation ?? 0;
        }
        public override float Progress
        {
            get => NatAssetSetting.AssetDelayTime <= 0 ? 1 : Mathf.Clamp01(_elapsed / NatAssetSetting.AssetDelayTime);
            protected set { }
        }
        internal override void TaskUpdate()
        {
            if (IsDone) return;
            SetTaskState(TaskState.Running);
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= NatAssetSetting.AssetDelayTime) Execute();
        }
        private void Execute()
        {
            RuntimeData.TryUnloadAsset(_target, _generation, this);
            SetTaskState(TaskState.End);
        }
        protected override void OnCancelTask() => SetTaskState(TaskState.End);
        protected override void OnSwitchToSync() { if (!IsDone) Execute(); }
        protected override void OnClear() { _target = null; _generation = 0; _elapsed = 0; }
    }
}