using System;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    /// <summary>WebGL loads plain bundles directly; encryption settings are ignored.</summary>
    internal class WebBundleTask : BaseTask
    {
        private UnityWebRequestAsyncOperation _op;
        private UnityWebRequest _req;
        private BundleInfo _lockedBundle;

        public override float Progress
        {
            get => IsDone ? 1f : _op?.progress ?? 0f;
            protected set => base.Progress = value;
        }

        protected override void OnCreate() { _taskType = TaskType.Bundle; }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
            {
                string path = RuntimeData.GetRuntimeLoadPath(TaskName);
                // Editor tests also exercise this loader with local bundle files.
                if (System.IO.Path.IsPathRooted(path) && !path.Contains("://"))
                    path = new Uri(path).AbsoluteUri;
                // WebGL always uses the manifest hash as the bundle version. The omitted
                // CRC defaults to zero; UseCRC and the legacy cache toggle do not apply here.
                _req = UnityWebRequestAssetBundle.GetAssetBundle(path, RuntimeData.GetAssetBundleHash(TaskName));
                _op = _req.SendWebRequest();
                _op.priority = (int)TaskPriority;
                SetTaskState(TaskState.Running);
            }

            if (TaskState != TaskState.Running) return;
            if (!_op.isDone) return;
            if (_req.result != UnityWebRequest.Result.Success)
            { Fail(_req.error); return; }

            var bundle = DownloadHandlerAssetBundle.GetContent(_req);
            DisposeRequest();
            Complete(bundle);
        }

        private void Complete(AssetBundle bundle)
        {
            if (bundle == null) { Fail("下载完成，但 AssetBundle 无法加载。"); return; }
            Result = bundle;
            _lockedBundle = BundleInfo.CreateBundleInfo(TaskName, bundle);
            RuntimeData.AddBundleInfo(_lockedBundle);
            _lockedBundle.Lock();
            _taskResult = TaskResult.Success;
            SetTaskState(TaskState.End);
        }

        private void Fail(string reason)
        {
            AbortPendingLoad();
            Debug.LogError($"Web资源包:{TaskName} 加载失败：{reason}");
            _taskResult = TaskResult.Faild;
            SetTaskState(TaskState.End);
        }

        protected override void OnChangePridrity()
        {
            if (_op != null) _op.priority = (int)TaskPriority;
        }

        protected override void OnCancelTask()
        {
            if (IsDone) return;
            AbortPendingLoad();
            _taskResult = TaskResult.Faild;
            SetTaskState(TaskState.End);
        }

        protected override void OnSwitchToSync()
        {
            if (!IsDone) Fail("WebGL 平台不允许同步加载，请使用异步加载 API。");
        }

        private void DisposeRequest()
        {
            if (_req != null)
            {
                if (!_req.isDone) _req.Abort();
                _req.Dispose();
            }
            _req = null;
            _op = null;
        }

        private void AbortPendingLoad()
        {
            if (_req != null && _req.isDone && _req.result == UnityWebRequest.Result.Success)
                DownloadHandlerAssetBundle.GetContent(_req)?.Unload(true);
            DisposeRequest();
        }

        protected override void OnClear()
        {
            AbortPendingLoad();
            _lockedBundle?.UnLock();
            _lockedBundle = null;
        }
    }
}
