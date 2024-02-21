using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class BundleDownloadTask : BaseTask
    {
        private readonly int _maxRetryCount = 3;
        private int _retryCount = 0;
        private UnityWebRequest _webRequest;
        private UnityWebRequestAsyncOperation _operation;
        private string _downLoadUri = string.Empty;
        private string _localFilePath = string.Empty;
        private string _tempLocalFilePath = string.Empty;
        //todo:bundleMainfest参数
        private BundleManifest _updateManifest;

        internal void SetWebDownLoadTaskParam(string downLoadUri, string localFilePath, string tempLocalFilePath, BundleManifest bundleManifest)
        {
            _downLoadUri = downLoadUri;
            _localFilePath = localFilePath;
            _tempLocalFilePath = tempLocalFilePath;
            _updateManifest = bundleManifest;
        }

        public override float Progress
        {
            get
            {
                if (_operation == null)
                {
                    return 0;
                }
                return _operation.webRequest.downloadProgress;
            }
            protected set => base.Progress = value;
        }


        protected override void OnCreate()
        {
            _taskType = TaskType.Web;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                ulong lastFileLength = 0;
                if (File.Exists(_tempLocalFilePath))
                {
                    FileInfo fileInfo = new FileInfo(_tempLocalFilePath);
                    lastFileLength = (ulong)fileInfo.Length;
                }

                //断点续传
                _webRequest = UnityWebRequest.Get(_downLoadUri);
                if (lastFileLength > 0)
                {
                    if (lastFileLength < _updateManifest.Length)
                    {
                        _webRequest.SetRequestHeader("Range", $"bytes={lastFileLength}-");
                    }
                    else
                    {
                        File.Delete(_tempLocalFilePath);
                        lastFileLength = 0;
                    }
                }

                _webRequest.downloadHandler = new DownloadHandlerFile(_tempLocalFilePath, lastFileLength > 0);
                _operation = _webRequest.SendWebRequest();
                _operation.priority = (int)TaskPriority;
                SetTaskState(TaskState.Running);
            }

            if (TaskState == TaskState.Running)
            {
                if (_operation.isDone)
                {
                    DownLoaded();
                }
            }

            if (TaskState == TaskState.End)
            {
            }
        }

        private void DownLoaded()
        {
            if (NatAssetRuntimeUtil.CheckWebRequestError(_webRequest))
            {
                //重新发起请求
                if (!TryToReSend())
                {
                    //超出重试上限
                    //todo:当完全失败时设置状态为结束，同时触发回调告知上层该bundle下载已失败
                    SetTaskState(TaskState.End);
                    _taskResult = TaskResult.Faild;
                }
                return;
            }
            bool isValid = NatAssetRuntimeUtil.CheckReadWriteBundle(_tempLocalFilePath, _updateManifest);
            if (!isValid)
            {
                File.Delete(_tempLocalFilePath);
                if(!TryToReSend())
                {
                    //超出重试上限
                    //todo:当完全失败时设置状态为结束，同时触发回调告知上层该bundle下载已失败
                    SetTaskState(TaskState.End);
                    _taskResult = TaskResult.Faild;
                }
                return;
            }

            SetTaskState(TaskState.End);

            if (File.Exists(_localFilePath))
            {
                File.Delete(_localFilePath);
            }
            File.Move(_tempLocalFilePath , _localFilePath);
            _taskResult = TaskResult.Success;
            //todo：告知外部该任务已执行完成
        }

        protected override void OnCancelTask()
        {
            if(TaskState == TaskState.Waiting)
            {
                SetTaskState(TaskState.End);
            }

            if (TaskState == TaskState.Running)
            {
                if (_webRequest != null)
                {
                    _webRequest.Dispose();
                    _webRequest = null;
                }
                _operation = null;
                SetTaskState(TaskState.End);
            }

            if(TaskState == TaskState.Finish)
            {
                SetTaskState(TaskState.End);
            }
        }

        protected override void OnSwitchToSync()
        {
            return;
        }

        protected override bool CanChangeRunModel()
        {
            return false;
        }

        protected override void OnChangePridrity()
        {
            if (_operation != null)
            {
                _operation.priority = (int)TaskPriority;
            }
        }
        protected override void OnClear()
        {
            if (_webRequest != null)
                _webRequest.Dispose();
            _retryCount = 0;
            _webRequest = null;
            _operation = null;
            _updateManifest = null;
            _downLoadUri = string.Empty;
            _localFilePath = string.Empty;
            _tempLocalFilePath = string.Empty;
        }

        private bool TryToReSend()
        {
            if (_retryCount == _maxRetryCount)
            {
                Debug.LogWarning($"TaskID:[{TaskGUID}];URL:[{_downLoadUri}] 请求失败，终止重试");
                return false;
            }

            _retryCount++;
            _webRequest.Dispose();
            _webRequest = null;
            _operation = null;
            Debug.LogWarning($"TaskID:[{TaskGUID}];URL:[{_downLoadUri}] 请求失败，第:{_retryCount + 1}次重新请求");
            SetTaskState(TaskState.Waiting);
            return true;
        }
    }
}