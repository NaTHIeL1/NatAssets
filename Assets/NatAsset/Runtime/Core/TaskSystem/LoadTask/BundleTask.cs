using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class BundleTask : BaseTask
    {
        private AssetBundle _assetBundle;
        private AssetBundleCreateRequest _bundleCreateRequest;

        public override float Progress
        {
            get
            {
                if (_bundleCreateRequest == null)
                    return 0;
                return _bundleCreateRequest.progress;
            }
            protected set => base.Progress = value;
        }
        protected override void OnCreate()
        {
            _taskType = TaskType.Bundle;
        }

        internal override void TaskUpdate()
        {
            //任务未启动
            if (TaskState == TaskState.Waiting)
            {
                string bundleName = TaskGUID;
                string loadPath = RuntimeData.GetRuntimeLoadPath(bundleName);
                uint crc = NatAssetSetting.UseCRC ? RuntimeData.GetBundleCRC(bundleName) : 0u;
                if (RunModel == RunModel.Async)
                {
                    BundleEncrypt bundleEncrypt = RuntimeData.GetBundleEncrypt(bundleName);
                    if (bundleEncrypt == BundleEncrypt.Nono)
                        _bundleCreateRequest =
                            AssetBundle.LoadFromFileAsync(loadPath, crc);
                    else if (bundleEncrypt == BundleEncrypt.Offset)
                        _bundleCreateRequest = AssetBundle.LoadFromFileAsync(loadPath, crc, EncryptionUtil.EncryptOffset);
                    _bundleCreateRequest.priority = (int)TaskPriority;
                    SetTaskState(TaskState.Running);
                }
                else
                {
                    //同步逻辑
                    BundleEncrypt bundleEncrypt = RuntimeData.GetBundleEncrypt(bundleName);
                    if (bundleEncrypt == BundleEncrypt.Nono)
                        _assetBundle = AssetBundle.LoadFromFile(loadPath, crc);
                    else if (bundleEncrypt == BundleEncrypt.Offset)
                        _assetBundle = AssetBundle.LoadFromFile(loadPath, crc, EncryptionUtil.EncryptOffset);
                    SetTaskState(TaskState.Finish);
                }
            }

            //执行加载
            if (TaskState == TaskState.Running)
            {
                if (_bundleCreateRequest.isDone)
                {
                    _assetBundle = _bundleCreateRequest.assetBundle;
                    SetTaskState(TaskState.Finish);
                }
            }

            //加载完毕
            if (TaskState == TaskState.Finish)
            {
                SetAssetBundle(_assetBundle);
                SetTaskState(TaskState.End);
            }

            //任务结束
            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        protected override void OnChangePridrity()
        {
            if (_bundleCreateRequest != null)
                _bundleCreateRequest.priority = (int)TaskPriority;
        }

        protected override void OnClear()
        {
            BundleInfo bundleInfo = RuntimeData.GetBundle(TaskGUID);
            if (bundleInfo != null)
            {
                //资源解锁
                bundleInfo.UnLock();
            }
            _bundleCreateRequest = null;
            _assetBundle = null;
        }

        protected override void OnCancelTask()
        {
            //未启动任务时取消
            if (TaskState == TaskState.Waiting)
            {
                SetTaskState(TaskState.End);
                return;
            }

            //加载中时取消
            if (TaskState == TaskState.Running)
            {
                _bundleCreateRequest.assetBundle.Unload(true);
                SetTaskState(TaskState.End);
                return;
            }

            //加载完时取消
            if (TaskState == TaskState.Finish)
            {
                if (_bundleCreateRequest != null)
                    _bundleCreateRequest.assetBundle.Unload(true);
                SetAssetBundle(null, true);
                SetTaskState(TaskState.End);
                return;
            }

            //任务处于结束状态
            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        protected override void OnSwitchToSync()
        {
            //未启动任务时
            if (TaskState == TaskState.Waiting)
            {
                //直接执行加载逻辑（同步加载）
                TaskUpdate();
                return;
            }

            //加载中时，打断异步加载转为同步
            if (TaskState == TaskState.Running)
            {
                //同步等待加载结果
                _assetBundle = _bundleCreateRequest.assetBundle;
                SetTaskState(TaskState.Finish);
                //执行后续逻辑
                TaskUpdate();
                return;
            }

            //加载完时
            if (TaskState == TaskState.Finish)
            {
                //执行后续逻辑
                TaskUpdate();
                return;
            }

            //任务处于结束状态,由框架层回收
            if (TaskState == TaskState.End)
            {
                return;
            }
        }

        private void SetAssetBundle(AssetBundle assetBundle, bool isRelease = false)
        {
            if (assetBundle == null)
            {
                if (isRelease) return;
                string Error = $"资源包:{TaskGUID} 加载失败，请检查加载路径是否正确";
                Debug.LogError(Error);
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