using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class GroupDownLoadProvider : BaseProvider
    {
        private GroupDownLoadProviderParam _groupParam;
        private string _groupName;
        private int recordIndex = 0;
        //下载速率
        private ulong _downLoadRate = 0;
        //上一次记录的长度
        private ulong _downLoadLastLength = 0;
        //总长度
        private ulong _downLoadTotalLength = 0;
        private float _recordTime = 0;
        private NatUpdaterInfo _natUpdaterInfo;
        private TaskRunner _netLoadTaskRunner;

        private List<CheckInfo> _checkInfos;
        //记录下载失败的bundle
        private List<string> _failDownLoadBundle;
        private List<BaseTask> _webDownLoadTask = new List<BaseTask>();
        private DownLoadState _downLoadState;

        /// <summary>
        /// 当前的下载速率，单位byte
        /// </summary>
        internal ulong DownLoadRate => _downLoadRate;
        /// <summary>
        /// 当前下载总长度，单位byte
        /// </summary>
        internal ulong DownLoadLength => _downLoadTotalLength;
        /// <summary>
        /// 获取下载失败的bundle列表
        /// </summary>
        internal List<string> FailDownLoadBundle => _failDownLoadBundle;

        protected enum DownLoadState
        {
            Running, //执行
            Pause,   //暂停
            Cancel,  //取消
        }

        protected override void OnCreate()
        {
            _netLoadTaskRunner = TaskSystem.NetLoadTaskRunner;
            _groupParam = (GroupDownLoadProviderParam)_providerParam;
            _natUpdaterInfo = _groupParam.NatUpdaterInfo;
            _groupName = AssetPath;
            _downLoadState = DownLoadState.Running;
        }

        internal override void OnUpdate()
        {
            if (ProviderState == ProviderState.Waiting)
            {
                SetProviderState(ProviderState.Running);
            }

            if (ProviderState == ProviderState.Running)
            {
                //节省内存
                switch (_downLoadState)
                {
                    case DownLoadState.Running:
                        if (_checkInfos == null)
                            _checkInfos = _natUpdaterInfo.GetCheckInfoByGroup(_groupName);
                        //有剩余可分配任务时分配任务
                        if (_netLoadTaskRunner.GetTaskNum() < NatAssetSetting.TaskFrameLimitNetLoad)
                        {
                            for (; recordIndex < _checkInfos.Count &&
                                _netLoadTaskRunner.GetTaskNum() < NatAssetSetting.TaskFrameLimitNetLoad;
                                recordIndex++)
                            {
                                CheckInfo checkInfo = _checkInfos[recordIndex];
                                CommonWebDownLoadTaskLogic(checkInfo, _netLoadTaskRunner, Priority, out BaseTask basetask);
                                if (basetask != null)
                                    SetWebDownLoadTaskLock(basetask.TaskGUID, basetask);
                            }
                        }
                        //检测是否有任务完成可释放
                        for (int i = 0; i < _webDownLoadTask.Count; i++)
                        {
                            BaseTask baseTask = _webDownLoadTask[i];
                            if (baseTask.IsDone)
                            {
                                if (!baseTask.IsSuccess)
                                {
                                    if (_failDownLoadBundle == null)
                                        _failDownLoadBundle = new List<string>();
                                    _failDownLoadBundle.Add(baseTask.TaskGUID);
                                }
                                SetWebDownLoadTaskUnLock(baseTask);
                                i--;
                            }
                        }

                        //检测是否完成全部下载任务
                        if (_webDownLoadTask.Count == 0 && recordIndex == _checkInfos.Count)
                        {
                            if (_failDownLoadBundle != null && _failDownLoadBundle.Count > 0)
                                SetProviderResult(ProviderResult.Faild);
                            else
                                SetProviderResult(ProviderResult.Success);
                            SetProviderState(ProviderState.Finish);
                        }
                        break;
                    case DownLoadState.Pause:
                        //处理暂停时要处理的问题
                        for (int i = 0; i < _webDownLoadTask.Count; i++)
                        {
                            BaseTask baseTask = _webDownLoadTask[i];
                            SetWebDownLoadTaskUnLock(baseTask);
                            if (baseTask.CheckCanDestroy())
                                baseTask.CancelTask();
                            i--;
                        }
                        //重置
                        recordIndex = 0;
                        break;
                    case DownLoadState.Cancel:
                        //如果进入了取消流程，但仍有新的句柄进入，就重新回归下载状态
                        if(Handles != null && Handles.Count > 0)
                        {
                            _downLoadState = DownLoadState.Running;
                            return;
                        }
                        //处理暂停时要处理的问题
                        for (int i = 0; i < _webDownLoadTask.Count; i++)
                        {
                            BaseTask baseTask = _webDownLoadTask[i];
                            SetWebDownLoadTaskUnLock(baseTask);
                            if (baseTask.CheckCanDestroy())
                                baseTask.CancelTask();
                            i--;
                        }
                        //重置
                        recordIndex = 0;
                        SetProviderState(ProviderState.Finish);
                        break;
                }
                CalculateRateAndLength();
            }

            if (ProviderState == ProviderState.Finish)
            {
                if (Handles != null)
                {
                    for (int i = 0; i < Handles.Count; i++)
                    {
                        UpdateHandle temp = Handles[i] as UpdateHandle;
                        if (temp != null)
                        {
                            temp.SetResult(this);
                        }
                    }
                }
                //todo:如果全部下载完毕，就写入当前最新的配置文件
            }
        }
        protected override void OnClear()
        {
            //确保任务全部解锁
            for (int i = 0; i < _webDownLoadTask.Count; i++)
            {
                BaseTask baseTask = _webDownLoadTask[i];
                baseTask.DesRefCount();
                if (baseTask.CheckCanDestroy())
                    baseTask.CancelTask();
            }
            _downLoadRate = 0;
            _downLoadLastLength = 0;
            _downLoadTotalLength = 0;
            _recordTime = 0;
            _natUpdaterInfo = null;
            _netLoadTaskRunner = null;
            _groupName = string.Empty;
            _groupParam = default;
            _webDownLoadTask.Clear();
            _failDownLoadBundle?.Clear();
            recordIndex = 0;
        }

        /// <summary>
        /// 网络下载不允许使用同步只能异步
        /// </summary>
        /// <returns></returns>
        protected override bool CanChangeLoadType()
        {
            return false;
        }

        protected override void OnChangeLoadType(RunModel runModel)
        {
            return;
        }

        protected override void OnChangePriority(Priority targetPriority)
        {
            SetOwnerAndDependPriority(targetPriority);
            if (_webDownLoadTask != null)
            {
                for (int i = 0; i < _webDownLoadTask.Count; i++)
                {
                    _webDownLoadTask[i].SetPriority(targetPriority);
                }
            }
        }

        protected override void OnCancelProvider()
        {
            base.OnCancelProvider();
            _downLoadState = DownLoadState.Cancel;
        }

        protected void CommonWebDownLoadTaskLogic(CheckInfo checkInfo, TaskRunner taskRunner, Priority priority, out BaseTask basetask)
        {
            if (checkInfo != null)
            {
                if (checkInfo.CheckNewIn == CheckNewIn.Remote)
                {
                    string relativePath = checkInfo.RemoteManifest.RelativePath;
                    BundleDownLoadTaskParam param = new BundleDownLoadTaskParam(checkInfo.CheckName, relativePath, checkInfo.RemoteManifest);
                    CommonTaskLogic<BundleDownloadTask>(param, taskRunner, priority, out basetask);
                }
            }
            basetask = null;
        }

        protected void SetWebDownLoadTaskLock(string fileName, BaseTask baseTask)
        {
            if (_webDownLoadTask == null)
                _webDownLoadTask = new List<BaseTask>();
            _webDownLoadTask.Add(baseTask);
            baseTask.AddRefCount();
        }

        protected void SetWebDownLoadTaskUnLock(BaseTask baseTask)
        {
            if (_webDownLoadTask == null) return;
            _webDownLoadTask.Remove(baseTask);
            baseTask.DesRefCount();
        }

        /// <summary>
        /// 暂停下载，由句柄触发
        /// </summary>
        internal void Pause()
        {
            _downLoadState = DownLoadState.Pause;
        }

        /// <summary>
        /// 恢复下载，有句柄触发
        /// </summary>
        internal void Resume()
        {
            if (_downLoadState == DownLoadState.Pause)
            {
                _downLoadState = DownLoadState.Running;
            }
        }

        private void CalculateRateAndLength()
        {
            for(int i = 0; i < _webDownLoadTask.Count; i++)
            {
                BundleDownloadTask task =  _webDownLoadTask[i] as BundleDownloadTask;
                if(task != null)
                {
                    _downLoadTotalLength += task.DeltaRecordByteLength;
                }
            }
            _recordTime += Time.deltaTime;
            if(_recordTime >= 1)
            {
                _downLoadRate = _downLoadTotalLength - _downLoadLastLength;
                _downLoadLastLength = _downLoadTotalLength;
                _recordTime = 0;
            }
        }
    }
}
