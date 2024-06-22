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

        protected override void OnCreate()
        {
            _assetProviderParam = (AssetProviderParam)_providerParam;
            _assetType = _assetProviderParam.AssetType;
            _localLoadRunner = TaskSystem.LoadTaskRunner;
            RuntimeData.GetBundlePath(AssetPath, out _bundleName, out _assetName);
            //todo:后续改为通过资源名获取资源依赖的bundle
            _dependenceBundles = RuntimeData.GetAllDependencies(_bundleName);
        }

        internal override void OnUpdate()
        {
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
                    SetAssetSetting(null);
                    SetProviderState(ProviderState.Finish);
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
                        if (_assetTask.IsSuccess)
                        {
                            //增加引用计数
                            AddBundlesRefCount();
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
                SetAssetHandle(asset, error, null);
                SetProviderResult(ProviderResult.Faild);
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
            SetAssetHandle(asset, error, assetInfo);
            SetProviderResult(ProviderResult.Success);
        }

        private void SetAssetHandle(object asset, string error, AssetInfo assetInfo)
        {
            if (error != String.Empty)
                Debug.LogErrorFormat(error);

            //当走完资源加载逻辑，但相应的发起句柄已全部卸载则立即启用资源卸载逻辑
            if (Handles == null || Handles.Count == 0)
            {
                assetInfo?.AddRefCount();
                assetInfo?.RedRefCount();
                return;
            }

            //正常情况下填充句柄
            for (int i = 0; i < Handles.Count; i++)
            {
                assetInfo?.AddRefCount();
                AssetHandle assetHandle = Handles[i] as AssetHandle;
                assetHandle.SetAsset(asset);
            }
        }

        private void AddBundlesRefCount()
        {
            if (_OwnerMainTask != null)
            {
                BundleInfo bundleInfo = RuntimeData.GetBundle(_bundleName);
                if (!ContainHasAddInfo(bundleInfo))
                    bundleInfo.AddRefCount();
            }

            foreach (string bundleStr in _dependenceBundles)
            {
                if (_DependenceTask.ContainsKey(bundleStr))
                {
                    BundleInfo tempBundle = RuntimeData.GetBundle(bundleStr);
                    if (!ContainHasAddInfo(tempBundle))
                        tempBundle.AddRefCount();
                }
            }
        }
    }
}