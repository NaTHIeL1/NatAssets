using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class AssetTask : BaseTask
    {
        private string _bundleName, _assetName;
        private AssetBundleRequest _assetBundleRequest;
        private object _asset;

        public override float Progress
        {
            get
            {
                if (_assetBundleRequest == null)
                    return 0;
                return _assetBundleRequest.progress;
            }
            protected set => base.Progress = value;
        }

        protected override void OnCreate()
        {
            _taskType = TaskType.Asset;
        }

        internal override void TaskUpdate()
        {
            //等待启动
            if (TaskState == TaskState.Waiting)
            {
                RuntimeData.GetBundlePath(TaskGUID, out _bundleName, out _assetName);
                BundleInfo bundleInfo = RuntimeData.GetBundle(_bundleName);
                if (bundleInfo == null)
                {
                    throw new Exception($"未获取到目标资源所属bundle:{_bundleName}");
                }
                else
                {
                    if (RunModel == RunModel.Async)
                    {
                        _assetBundleRequest = bundleInfo.Bundle.LoadAssetAsync(_assetName);
                        _assetBundleRequest.priority = (int)TaskPriority;
                        SetTaskState(TaskState.Running);
                    }
                    else
                    {
                        _asset = bundleInfo.Bundle.LoadAsset(_assetName);
                        SetTaskState(TaskState.Finish);
                    }
                }
            }

            //执行加载
            if (TaskState == TaskState.Running)
            {
                if (_assetBundleRequest.isDone)
                {
                    _asset = _assetBundleRequest.asset;
                    SetTaskState(TaskState.Finish);
                }
            }

            //加载完成
            if (TaskState == TaskState.Finish)
            {
                SetAssetInfo(_asset);
                SetTaskState(TaskState.End);
            }

            //任务结束
            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        protected override void OnClear()
        {
            _bundleName = string.Empty;
            _assetName = string.Empty;
            _asset = null;
            _assetBundleRequest = null;
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
                _asset = null;
                SetTaskState(TaskState.End);
                return;
            }

            if (TaskState == TaskState.Finish)
            {
                SetAssetInfo(null, true);
                SetTaskState(TaskState.End);
                return;
            }

            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        protected override void OnChangePridrity()
        {
            if (_assetBundleRequest != null)
                _assetBundleRequest.priority = (int)TaskPriority;
        }

        protected override void OnSwitchToSync()
        {
            if (TaskState == TaskState.Waiting)
            {
                TaskUpdate();
                return;
            }

            if (TaskState == TaskState.Running)
            {
                _asset = _assetBundleRequest.asset;
                SetTaskState(TaskState.Finish);
                TaskUpdate();
                return;
            }

            if (TaskState == TaskState.Finish)
            {
                //执行后续逻辑
                TaskUpdate();
                return;
            }

            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        private void SetAssetInfo(object asset, bool isRelease = false)
        {
            if (asset == null)
            {
                if (isRelease) return;
                Debug.LogError($"资源路径:{TaskGUID},加载资源资源名:{_assetName}时出错，检查是否资源名错误");
                _taskResult = TaskResult.Faild;
            }
            else
            {
                Result = asset;
                _taskResult = TaskResult.Success;
            }
        }
    }
}