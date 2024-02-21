using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class AssetUnLoadTask : BaseTask
    {
        private float executionTime = 0;

        protected override void OnCreate()
        {
            executionTime = 0;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
                SetTaskState(TaskState.Running);

            if (TaskState == TaskState.Running)
            {
                executionTime += Time.deltaTime;
#if UNITY_EDITOR
                Progress = executionTime / NatAssetSetting.AssetDelayTime;
#endif
                if (executionTime >= NatAssetSetting.AssetDelayTime)
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
            }

            if (TaskState == TaskState.Running)
            {
                executionTime = 0;
                Progress = 0;
                SetTaskState(TaskState.End);
            }

            if (TaskState == TaskState.End)
                return;
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
            executionTime = 0;
        }

        private void UnLoad()
        {
            AssetInfo _assetInfo = RuntimeData.GetAsset(TaskGUID);
            //卸载原生资源
            if (_assetInfo.Asset is Sprite sprite)
                Resources.UnloadAsset(sprite.texture);
            else if (_assetInfo.Asset is Texture2D || _assetInfo.Asset is TextAsset)
                Resources.UnloadAsset(_assetInfo.Asset as Object);
            //原生资源
            //Debug.Log($"资源{_assetInfo.targetPath}卸载成功");
            RuntimeData.RemoveAssetInfo(_assetInfo);
            BundleInfo.Release(_assetInfo);
        }
    }
}