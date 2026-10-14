using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class WebRequestTask : BaseTask
    {
        private static uint _webTaskGUID;
        private int _maxRetryCount;
        private int _retryCount;
        private bool _completed;
        private UnityWebRequest _webRequest;
        private UnityWebRequestAsyncOperation _operation;
        private Action<string, bool, UnityWebRequest> _callback;

        public override float Progress { get => _operation?.progress ?? 0; protected set => base.Progress = value; }

        internal static WebRequestTask Create(string url, Priority priority, Action<string, bool, UnityWebRequest> callback, int retryCount = -1)
        {
            return CreateTask<WebRequestTask>(new WebTaskParam((++_webTaskGUID).ToString(), url, retryCount, callback), priority);
        }

        protected override void OnCreate()
        {
            _taskType = TaskType.Web;
            var param = (WebTaskParam)_taskParam;
            _maxRetryCount = param.RetryCount < 0 ? 3 : param.RetryCount;
            _retryCount = 0;
            _completed = false;
            _callback = param.CallBack;
        }

        internal override void TaskUpdate()
        {
            if (_completed) return;
            if (TaskState == TaskState.Waiting)
            {
                try
                {
                    string url = Path.IsPathRooted(TaskName) ? new Uri(Path.GetFullPath(TaskName)).AbsoluteUri : TaskName;
                    _webRequest = UnityWebRequest.Get(url);
                    _operation = _webRequest.SendWebRequest();
                    _operation.priority = (int)TaskPriority;
                    SetTaskState(TaskState.Running);
                }
                catch (Exception error)
                {
                    Debug.LogError($"[NatAsset] 请求无法启动：{TaskName}；{error.Message}");
                    Complete(false);
                    return;
                }
            }
            if (TaskState != TaskState.Running || !_operation.isDone) return;
            if (_webRequest.result == UnityWebRequest.Result.Success)
                Complete(true);
            else if (_retryCount++ < _maxRetryCount)
            {
                _webRequest.Dispose();
                _webRequest = null;
                _operation = null;
                SetTaskState(TaskState.Waiting);
            }
            else Complete(false);
        }

        private void Complete(bool success)
        {
            if (_completed) return;
            _completed = true;
            _taskResult = success ? TaskResult.Success : TaskResult.Faild;
            SetTaskState(TaskState.End);
            var callback = _callback;
            _callback = null;
            if (callback != null)
                CompletionCallbacks.Run(() => callback(TaskGUID, success, _webRequest), TaskName);
        }

        protected override void OnCancelTask()
        {
            _completed = true;
            _callback = null;
            _taskResult = TaskResult.Faild;
            _webRequest?.Abort();
            _webRequest?.Dispose();
            _webRequest = null;
            _operation = null;
            SetTaskState(TaskState.End);
        }

        protected override void OnSwitchToSync() { }
        protected override bool CanChangeRunModel() => false;
        protected override void OnClear()
        {
            _webRequest?.Dispose();
            _webRequest = null;
            _operation = null;
            _callback = null;
            _completed = false;
            _maxRetryCount = 3;
            _retryCount = 0;
        }
    }
}
