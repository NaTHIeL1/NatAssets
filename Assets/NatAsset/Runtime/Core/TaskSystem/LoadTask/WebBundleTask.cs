using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    /// <summary>
    /// webgl平台的bundle加载任务
    /// </summary>
    internal class WebBundleTask : BaseTask
    {
        private AssetBundle _assetBundle;
        private UnityWebRequestAsyncOperation _op;
        private UnityWebRequest _req;

        public override float Progress
        {
            get
            {
                if (_op == null)
                    return 0;
                return _op.progress;
            }
            protected set => base.Progress = value;
        }

        protected override void OnCreate()
        {
            _taskType = TaskType.Bundle;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                string bundleName = TaskGUID;
                string targetURL = Path.Combine(NatAssetSetting.AssetServerURL, bundleName);
                if (NatAssetSetting.WebGLIsUseCache)
                    _req = UnityWebRequestAssetBundle.GetAssetBundle(targetURL,
                        RuntimeData.GetAssetBundleHash(bundleName));
                else
                    _req = UnityWebRequestAssetBundle.GetAssetBundle(targetURL);
                _op = _req.SendWebRequest();
                _op.priority = (int) TaskPriority;
            }

            if (TaskState == TaskState.Running)
            {
                if (_op.webRequest.isDone)
                {
                    if (_op.webRequest.result != UnityWebRequest.Result.Success)
                    {
                        SetAssetBundle(null);
                        SetTaskState(TaskState.End);
                    }
                    else
                    {
                        _assetBundle = DownloadHandlerAssetBundle.GetContent(_op.webRequest);
                        SetAssetBundle(_assetBundle);
                        SetTaskState(TaskState.End);
                    }
                }
            }
        }

        protected override void OnClear()
        {
            _assetBundle = null;
            if (_req != null)
                _req.Dispose();
            _op = null;
            _req = null;
        }

        protected override void OnCancelTask()
        {
        }

        protected override void OnSwitchToSync()
        {
            if (IsDone == false)
            {
                string error = $"{nameof(WebBundleTask)}加载失败，WebGL平台不允许同步加载";
                Debug.LogError(error);
                _taskResult = TaskResult.Faild;
                SetTaskState(TaskState.End);
            }
        }

        private void SetAssetBundle(AssetBundle assetBundle, bool isRelease = false)
        {
            if (assetBundle == null)
            {
                if (isRelease) return;
                string error = $"Web资源包:{TaskGUID} 加载失败，加载路径错误或网络状况差";
                Debug.LogError(error);
                _taskResult = TaskResult.Faild;
            }
            else
            {
                Result = assetBundle;
                BundleInfo bundleInfo = BundleInfo.CreateBundleInfo(TaskGUID, assetBundle);
                RuntimeData.AddBundleInfo(bundleInfo);
                //资源上锁
                bundleInfo.Lock();
                _taskResult = TaskResult.Success;
            }
        }
    }
}