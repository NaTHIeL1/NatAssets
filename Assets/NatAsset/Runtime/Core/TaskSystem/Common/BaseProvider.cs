using System;
using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    /// <summary>
    /// 在BaseProvider中处理加载逻辑
    /// Provider相当于加载逻辑中枢，负责调度Task，处理句柄，处理引用计数
    /// </summary>
    public abstract class BaseProvider : IReference
    {
        //记录上游句柄
        private List<IHandle> _handles;
        private List<BaseInfo> _infos;

        protected string _providerGUID;
        protected string _assetPath;
        protected bool _isSceneProvider = false;
        protected IProviderParam _providerParam;
        protected BaseTask _OwnerMainTask;
        protected DependenciesTask _DependenceTask = new DependenciesTask();

        private RunModel _runModel = RunModel.Async;
        private Priority _priority = Priority.Low;
        private ProviderState _providerState = ProviderState.Waiting;
        private ProviderResult _providerResult = ProviderResult.Nono;

        public RunModel RunModel => _runModel;
        public Priority Priority => _priority;
        public ProviderState ProviderState => _providerState;
        public ProviderResult ProviderResult => _providerResult;
        public string ProviderGUID => _providerGUID;
        public string AssetPath => _assetPath;

        public bool IsSceneProvider => _isSceneProvider;

        public List<IHandle> Handles => _handles;

        public bool IsLoading => _providerState == ProviderState.Running;
        public bool IsDone => _providerState == ProviderState.Finish;

        public static T Create<T>(IProviderParam providerParam, Priority priority) where T : BaseProvider, new()
        {
            T loadTask = ReferencePool.Get<T>();
            loadTask._providerParam = providerParam;
            loadTask._providerGUID = providerParam.ProviderGUID;
            loadTask._assetPath = providerParam.AssetPath;
            loadTask._runModel = RunModel.Async;
            loadTask._priority = priority;
            loadTask.OnCreate();
            return loadTask;
        }

        public static void Release(BaseProvider baseProvider)
        {
            ReferencePool.Release(baseProvider);
        }

        public void Clear()
        {
            OnClear();
            ReduceRefCountOnLoadFail();
            if (_handles != null)
            {
                foreach (IHandle handle in _handles)
                {
                    handle.SetProvider(null);
                }

                _handles.Clear();
            }

            _providerResult = ProviderResult.Nono;
            _isSceneProvider = false;
            _OwnerMainTask = null;
            _providerGUID = null;
            _providerState = ProviderState.Waiting;
            _priority = Priority.Low;
            _runModel = RunModel.Async;
            _providerParam = null;
        }

        /// <summary>
        /// Provider创建
        /// </summary>
        protected abstract void OnCreate();

        /// <summary>
        /// Provider轮询
        /// </summary>
        internal abstract void OnUpdate();

        /// <summary>
        /// Provider回收
        /// </summary>
        protected abstract void OnClear();

        /// <summary>
        /// 只切换至同步加载状态
        /// </summary>
        /// <param name="runModel"></param>
        protected abstract void OnChangeLoadType(RunModel runModel);

        /// <summary>
        /// 只向高优先级切换
        /// </summary>
        /// <param name="targetPriority"></param>
        protected abstract void OnChangePriority(Priority targetPriority);

        /// <summary>
        /// 需要在handle全部移除的情况下立即执行取消逻辑的Provider重载
        /// </summary>
        protected virtual void OnCancelProvider()
        {
        }

        protected void SetProviderState(ProviderState providerState)
        {
            _providerState = providerState;
        }

        protected void SetProviderResult(ProviderResult providerResult)
        {
            _providerResult = providerResult;
        }

        protected bool IsDoneOwnerAndDepend()
        {
            if (_OwnerMainTask != null && !_OwnerMainTask.IsDone)
            {
                return false;
            }

            if (!_DependenceTask.IsDone())
            {
                return false;
            }

            return true;
        }

        protected bool IsSuccessOwnerAndDepend()
        {
            if (_OwnerMainTask != null && !_OwnerMainTask.IsSuccess)
            {
                return false;
            }

            if (!_DependenceTask.IsSuccess())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 切换优先级
        /// </summary>
        /// <param name="priority"></param>
        protected void SetOwnerAndDependPriority(Priority priority)
        {
            if (_OwnerMainTask != null)
                _OwnerMainTask.SetPriority(priority);
            if (_DependenceTask != null)
                _DependenceTask.SetPriority(priority);
        }

        /// <summary>
        /// 切换加载方式
        /// </summary>
        /// <param name="runModel"></param>
        protected void SetOwnerAndDependRunModel(RunModel runModel)
        {
            if (_OwnerMainTask != null)
                _OwnerMainTask.SetRunMode(runModel);
            if (_DependenceTask != null)
                _DependenceTask.SetRunModel(runModel);
        }

        /// <summary>
        /// 释放引用计数
        /// </summary>
        protected void SetOwnerAndDependRelease()
        {
            if (_OwnerMainTask != null)
                _OwnerMainTask.DesRefCount();
            if (_DependenceTask != null)
                _DependenceTask.Release();
        }

        public void SetPriority(Priority priority)
        {
            if (this._priority < priority)
            {
                this._priority = priority;
                OnChangePriority(priority);
            }
        }

        public void SetLoadType(RunModel runModel)
        {
            if (CanChangeLoadType())
            {
                if (this._runModel == RunModel.Sync) return;
                if (runModel == RunModel.Sync)
                {
                    this._runModel = runModel;
                    OnChangeLoadType(runModel);
                }
            }
        }

        protected virtual bool CanChangeLoadType()
        {
            return true;
        }

        /// <summary>
        /// 添加引用句柄
        /// </summary>
        /// <param name="handle"></param>
        public void AddHandle(IHandle handle, Priority priority, RunModel runModel)
        {
            if (_handles == null)
                _handles = new List<IHandle>();
            _handles.Add(handle);
            handle.SetProvider(this);
            SetPriority(priority);
            SetLoadType(runModel);
        }

        /// <summary>
        /// 移除句柄
        /// </summary>
        /// <param name="handle"></param>
        public void RemHandle(IHandle handle)
        {
            if (handle == null) return;
            _handles.Remove(handle);
            if (_handles.Count == 0)
                OnCancelProvider();
        }

        private void AddInfo(BaseInfo baseInfo)
        {
            if (_infos == null)
                _infos = new List<BaseInfo>();
            _infos.Add(baseInfo);
        }

        private void Remove(BaseInfo baseInfo)
        {
            if (_infos == null) return;
            _infos.Remove(baseInfo);
        }

        private void ReduceRefCountOnLoadFail()
        {
            if (_providerResult == ProviderResult.Success) return;
            if (_infos == null) return;
            for (int i = 0; i < _infos.Count; i++)
            {
                _infos[i].RedRefCount();
            }
        }

        protected bool ContainHasAddInfo(BaseInfo baseInfo)
        {
            if (_infos == null)
            {
                return false;
            }

            return _infos.Contains(baseInfo);
        }

        protected bool CommonAssetTaskLogic(string assetName,Type type, TaskRunner taskRunner, Priority priority,
            out BaseTask baseTask)
        {
            AssetTaskParam assetTaskParam = new AssetTaskParam(assetName, type);
            CommonTaskLogic<AssetTask>(assetTaskParam, taskRunner, priority, out baseTask);
            return true;
        }

        protected bool CommonBundleTaskLogic(string bundleName, TaskRunner taskRunner, Priority priority,
            out BaseTask baseTask)
        {
            BundleInfo bundleInfo = RuntimeData.GetBundle(bundleName);
            if (bundleInfo != null)
            {
                AddInfo(bundleInfo);
                bundleInfo.AddRefCount();
                baseTask = null;
                return false;
            }
            else
            {
                ComTaskParam comTaskParam = new ComTaskParam(bundleName, bundleName);
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    CommonTaskLogic<WebBundleTask>(comTaskParam, taskRunner, priority, out baseTask);
                }
                else
                {
                    CommonTaskLogic<BundleTask>(comTaskParam, taskRunner, priority, out baseTask);
                }
                return true;
            }
        }

        protected void CommonTaskLogic<T>(ITaskParam taskParam, TaskRunner taskRunner, Priority priority,
            out BaseTask baseTask) where T : BaseTask, new()
        {
            if (taskRunner.TryGetValue(taskParam.TaskGUID, out BaseTask tempTask))
            {
                baseTask = (T) tempTask;
                baseTask.AddRefCount();
            }
            else
            {
                baseTask = BaseTask.CreateTask<T>(taskParam, priority);
                taskRunner.AddTask(baseTask);
                baseTask.AddRefCount();
            }
        }
    }
}