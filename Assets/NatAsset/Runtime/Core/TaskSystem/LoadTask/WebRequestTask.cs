﻿using System;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class WebRequestTask : BaseTask
    {
        private string _url;
        private int _maxRetryCount = 3;
        private int _retryCount = 0;
        private UnityWebRequest _webRequest;
        private UnityWebRequestAsyncOperation _operation;
        private Action<bool, UnityWebRequest> _callback;

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

        internal static WebRequestTask Create(string url, Priority priority, Action<bool, UnityWebRequest> callback, int retryCount = -1)
        {
            WebRequestTask webRequestTask = WebRequestTask.CreateTask<WebRequestTask>(string.Empty, priority);
            webRequestTask._callback = callback;
            webRequestTask._url = url;
            if(retryCount > 0 )
            {
                webRequestTask._maxRetryCount = retryCount;
            }
            return webRequestTask;
        }

        protected override void OnCreate()
        {
            _taskType = TaskType.Web;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                _webRequest = UnityWebRequest.Get(_url);
                _operation = _webRequest.SendWebRequest();
                _operation.priority = (int) TaskPriority;
                SetTaskState(TaskState.Running);
            }

            if (TaskState == TaskState.Running)
            {
 
                if (_operation.isDone)
                {
                    if (_webRequest.result == UnityWebRequest.Result.Success)
                    {
                        _callback(true, _webRequest);
                        SetTaskState(TaskState.End);
                    }
                    else
                    {
                        //重新发起请求
                        if (!TryToReSend())
                        {
                            _callback(false, _webRequest);
                            SetTaskState(TaskState.End);
                        }
                    }
                }
            }

            if (TaskState == TaskState.End)
            {
            }
        }

        protected override void OnCancelTask()
        {
            if (TaskState == TaskState.Waiting)
            {
                SetTaskState(TaskState.End);
            }

            if (TaskState == TaskState.Running)
            {
                if (_webRequest != null)
                    _webRequest.Dispose();
                _webRequest = null;
                _operation = null;
                SetTaskState(TaskState.End);
            }
        }

        protected override void OnSwitchToSync()
        {
            
        }

        protected override bool CanChangeRunModel()
        {
            return false;
        }

        protected override void OnClear()
        {
            _webRequest.Dispose();
            _retryCount = 0;
            _webRequest = null;
            _callback = null;
            _operation = null;
            _url = null;
        }

        private bool TryToReSend()
        {
            if (_retryCount == _maxRetryCount)
            {
                Debug.LogWarning($"[{_url}]请求失败，终止重试");
                return false;
            }

            _retryCount++;
            _webRequest.Dispose();
            _webRequest = null;
            _operation = null;
            Debug.LogWarning($"[{_url}]请求失败，第:{_retryCount + 1}次重新请求");
            SetTaskState(TaskState.Waiting);
            return true;
        }
    }
}