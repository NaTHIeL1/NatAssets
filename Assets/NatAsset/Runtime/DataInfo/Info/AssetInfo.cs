using System.Collections.Generic;
using Object = System.Object;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class AssetInfo : BaseInfo
    {
        internal object Asset { get; private set; }
        internal List<string> DepBundles { get; private set; }

        private void CreateInfo(Object asset, List<string> bundles)
        {
            this.Asset = asset;
            this.DepBundles = bundles;
        }

        internal static AssetInfo CreateAssetInfo(string targetPath, Object asset, List<string> depBundle)
        {
            AssetInfo assetInfo = AssetInfo.Create<AssetInfo>(targetPath);
            assetInfo.CreateInfo(asset, depBundle);
            return assetInfo;
        }

        protected override void OnAddRefCount()
        {
            //Add时为0代表该资源正处于卸载计时状态，需要取消计时
            if (RefCount == 0)
            {
                //取消卸载任务
                CancelUnLoadTask();
            }
        }

        protected override void OnRedRefCount()
        {
            if (CheckNeedUnLoadInfo())
            {
                //启动卸载任务
                LaunchUnLoadTask();
            }
        }

        protected override void OnUnLock()
        {
            if (CheckNeedUnLoadInfo())
            {
                //启动卸载任务
                LaunchUnLoadTask();
            }
        }
        
        private void LaunchUnLoadTask()
        {
            if (_unLoadTask != null) return;
            _unLoadTask = AssetUnLoadTask.CreateTask<AssetUnLoadTask>(InfoNameGUID, Priority.Middle);
            TaskSystem.UnLoadTaskRunner.AddTask(_unLoadTask);
        }

        private void CancelUnLoadTask()
        {
            if (_unLoadTask != null)
            {
                _unLoadTask.CancelTask();
            }

            _unLoadTask = null;
        }

        protected override void OnClear()
        {
            //判空用于处理编辑器模式
#if UNITY_EDITOR
            if (NatAssetSetting.TRunWay == RunWay.PackageOnly)
            {
                OnToReleaseScene();
            }
#else
            OnToReleaseScene();
#endif
            DepBundles = null;
        }

        private void OnToReleaseScene()
        {
            if (DepBundles == null) return;
            foreach (string bundle in DepBundles)
            {
                RuntimeData.GetBundle(bundle).RedRefCount();
            }
        }
    }
}