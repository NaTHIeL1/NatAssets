using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public class RawAssetTask : BaseTask
    {
        private UnityWebRequest _webRequest;
        private UnityWebRequestAsyncOperation _operation;

        public override float Progress
        {
            get
            {
                if (_operation == null)
                    return 0;
                return _operation.progress;
            }
            protected set => base.Progress = value;
        }

        protected override void OnCreate()
        {
            _taskType = TaskType.Asset;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                string assetPath = RuntimeData.GetRuntimeLoadPath(TaskGUID);
                _webRequest = UnityWebRequest.Get(assetPath);
                _operation = _webRequest.SendWebRequest();
                if (RunModel == RunModel.Async)
                {
                    SetTaskState(TaskState.Running);
                }
                else
                {
                    SetToSync(_webRequest);
                    SetTaskState(TaskState.End);
                }
            }
            if (TaskState == TaskState.Running)
            {
                if (_operation.isDone)
                {
                    SetTextAssetInfo(_webRequest);
                    SetTaskState(TaskState.End);
                }
            }
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
                _webRequest.Dispose();
                SetTaskState(TaskState.End);
                return;
            }
        }

        protected override void OnSwitchToSync()
        {
            if (TaskState == TaskState.Waiting)
            {
                TaskUpdate();
                SetTaskState(TaskState.End);
                return;
            }
            if (TaskState == TaskState.Running)
            {
                SetToSync(_webRequest);
                SetTaskState(TaskState.End);
                return;
            }
        }

        protected override void OnClear()
        {
            if (_webRequest != null)
                _webRequest.Dispose();
            _webRequest = null;
            _operation = null;
        }

        private void SetTextAssetInfo(UnityWebRequest unityWebRequest)
        {
            if (unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                Result = unityWebRequest.downloadHandler.data;
                _taskResult = TaskResult.Success;
            }
            else
            {
                _taskResult = TaskResult.Faild;
            }
        }

        private void SetToSync(UnityWebRequest unityWebRequest)
        {
            while (true)
            {
                if (unityWebRequest.isDone)
                {
                    SetTextAssetInfo(_webRequest);
                    break;
                }
            }
        }
    }
}