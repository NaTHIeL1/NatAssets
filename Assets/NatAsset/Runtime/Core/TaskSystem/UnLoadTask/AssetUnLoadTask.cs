﻿using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class AssetUnLoadTask : BaseTask
    {
        private float _executionTime = 0;
        private Type _assetType;
        private AssetTaskParam _assetTaskParam;

        protected override void OnCreate()
        {
            _executionTime = 0;
            _assetTaskParam = (AssetTaskParam)_taskParam;
            _assetType = _assetTaskParam.AssetType;
        }

        public override float Progress
        {
            get
            {
                return _executionTime / NatAssetSetting.AssetDelayTime;
            }
            protected set => base.Progress = value;
        }

        internal override void TaskUpdate()
        {
            if (TaskState == TaskState.Waiting)
                SetTaskState(TaskState.Running);

            if (TaskState == TaskState.Running)
            {
                _executionTime += Time.deltaTime;
                if (_executionTime >= NatAssetSetting.AssetDelayTime)
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
                _executionTime = 0;
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
            _executionTime = 0;
            _assetType = null;
            _assetTaskParam = default;
        }

        private void UnLoad()
        {
            AssetInfo _assetInfo = RuntimeData.GetAsset(TaskName, _assetType);
            //卸载原生资源
            if (_assetInfo.Asset is Sprite sprite)
                Resources.UnloadAsset(sprite.texture);
            else if (_assetInfo.Asset is Texture2D || _assetInfo.Asset is TextAsset)
                Resources.UnloadAsset(_assetInfo.Asset as UnityEngine.Object);
            //原生资源
            //Debug.Log($"资源{_assetInfo.targetPath}卸载成功");
            RuntimeData.RemoveAssetInfo(_assetInfo);
            BundleInfo.Release(_assetInfo);
        }
    }
}