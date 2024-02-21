using System;

#if UNITY_EDITOR
namespace NATFrameWork.NatAsset.Runtime
{
    [Obsolete("供编辑器使用")]
    public struct LoadTaskInfo
    {
        public string TaskName;
        public TaskType TaskType;
        public TaskState TaskState;
        public int DownStreamingNum;

        public void SetParam(BaseTask loadTask)
        {
            TaskName = loadTask.TaskGUID;
            TaskType = loadTask.TaskType;
            TaskState = loadTask.TaskState;
        }

        public void SetDownStreamingNum(int downStreamingNum)
        {
            DownStreamingNum = downStreamingNum;
        }
    }

    [Obsolete("供编辑器使用")]
    public struct UnLoadTaskInfo
    {
        public TaskType TaskType;
        public string TaskName;
        public TaskState TaskState;
        public float TaskProcess;

        public void SetParam(BaseTask unLoadTask)
        {
            TaskName = unLoadTask.TaskGUID;
            TaskState = unLoadTask.TaskState;
            if (unLoadTask is AssetUnLoadTask)
            {
                TaskType = TaskType.Asset;
                TaskProcess = unLoadTask.Progress;
            }
            else if (unLoadTask is BundleUnLoadTask)
            {
                TaskType = TaskType.Bundle;
                TaskProcess = unLoadTask.Progress;
            }
        }
    }
}
#endif