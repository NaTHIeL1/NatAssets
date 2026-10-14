using System;
using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class AssetProvider : BaseProvider
    {
        private AssetTask _assetTask = null;
        private string _bundleName, _assetName;
        private string[] _dependenceBundles;
        private TaskRunner _localLoadRunner;
        private Type _assetType;
        private AssetProviderParam _assetProviderParam;
        private AssetLifetimeInfo _loadingLifetime;
        private string _loadError;

        protected override void OnCreate()
        {
            _assetProviderParam = (AssetProviderParam)_providerParam;
            _assetType = _assetProviderParam.AssetType;
            try { _loadingLifetime = RuntimeData.AcquireAssetLoad(AssetPath, false); }
            catch (Exception error) { _loadError = error.Message; return; }
            _localLoadRunner = TaskSystem.LoadTaskRunner;
            RuntimeData.GetBundlePath(AssetPath, out _bundleName, out _assetName);
            //todo:后续改为通过资源名获取资源依赖的bundle
            _dependenceBundles = RuntimeData.GetAllDependencies(_bundleName);
        }

        internal override void OnUpdate()
        {
            if (_loadError != null)
            {
                SetProviderState(ProviderState.Finish);
                SetProviderResult(ProviderResult.Faild);
                CompleteAssetHandles(null, _loadError, null);
                return;
            }
            //Provider未启动
            if (ProviderState == ProviderState.Waiting)
            {
                StartTaskOnAwake();
                SetProviderState(ProviderState.Running);
            }

            //执行中
            if (ProviderState == ProviderState.Running)
            {
                //依赖任务未完成
                if (!IsDoneOwnerAndDepend())
                    return;
                //依赖任务全部成功
                if (!IsSuccessOwnerAndDepend())
                {
                    SetProviderState(ProviderState.Finish);
                    SetAssetSetting(null);
                }
                else
                {
                    //启动正式资源加载
                    if (_assetTask == null)
                    {
                        CommonAssetTaskLogic(_assetName, _assetType, _localLoadRunner, Priority, out BaseTask baseTask);
                        _assetTask = (AssetTask) baseTask;
                    }

                    _assetTask.SetRunMode(RunModel);

                    //判断资源加载进度
                    if (_assetTask.IsDone)
                    {
                        SetProviderState(ProviderState.Finish);
                        if (_assetTask.IsSuccess)
                        {
                            //增加引用计数
                            SetAssetSetting(_assetTask.Result);
                        }
                        else
                        {
                            SetAssetSetting(null);
                        }

                        SetProviderState(ProviderState.Finish);
                    }
                }
            }

            //执行完毕
            if (ProviderState == ProviderState.Finish)
            {
            }
        }

        protected override void OnClear()
        {
            RuntimeData.ReleaseAssetLoad(_loadingLifetime);
            _loadingLifetime = null;
            _loadError = null;
            //解除任务锁定
            SetOwnerAndDependRelease();
            if (_assetTask != null)
            {
                _assetTask.DesRefCount();
            }

            _assetTask = null;
            _bundleName = string.Empty;
            _assetName = string.Empty;
            _localLoadRunner = null;
            _assetType = null;
            _assetProviderParam = default;
            _dependenceBundles = null;
        }

        protected override void OnChangeLoadType(RunModel runModel)
        {
            //未启动加载则直接启动
            if (ProviderState == ProviderState.Waiting)
            {
                OnUpdate();
                return;
            }

            //执行检测中
            if (ProviderState == ProviderState.Running)
            {
                //切换加载方式
                SetOwnerAndDependRunModel(runModel);
                OnUpdate();
                return;
            }

            //此时任务已结束
            if (ProviderState == ProviderState.Finish)
            {
                return;
            }
        }

        protected override void OnChangePriority(Priority targetPriority)
        {
            if (ProviderState == ProviderState.Running)
            {
                SetOwnerAndDependPriority(Priority);
            }
        }

        private void StartTaskOnAwake()
        {
            //资源直接依赖的bundle
            CommonBundleTaskLogic(_bundleName, _localLoadRunner, Priority, out _OwnerMainTask);

            //bundle依赖的其他bundle
            foreach (string bundleStr in _dependenceBundles)
            {
                BaseTask tempBaseTask = null;
                if (CommonBundleTaskLogic(bundleStr, _localLoadRunner, Priority, out tempBaseTask))
                {
                    _DependenceTask.AddTask(tempBaseTask);
                }
            }

            SetOwnerAndDependPriority(Priority);
            SetOwnerAndDependRunModel(RunModel);
        }

        private void SetAssetSetting(object asset)
        {
            string error = string.Empty;
            if (asset == null)
            {
                error = $"资源路径:{AssetPath},加载资源资源名:{_assetName}时出错，检查是否资源名错误";
                SetProviderResult(ProviderResult.Faild);
                SetAssetHandle(asset, error, null);
                return;
            }

            List<string> bundles = new List<string>();
            bundles.Add(_bundleName);
            foreach (string bundle in _dependenceBundles)
            {
                bundles.Add(bundle);
            }

            AssetInfo assetInfo = AssetInfo.CreateAssetInfo(AssetPath, _assetType, asset, bundles);
            RuntimeData.AddAssetInfo(assetInfo);
            SetProviderResult(ProviderResult.Success);
            SetAssetHandle(asset, error, assetInfo);
        }

        private void SetAssetHandle(object asset, string error, AssetInfo info)
        {
            CompleteAssetHandles(asset, error, info);
        }
    }
}
