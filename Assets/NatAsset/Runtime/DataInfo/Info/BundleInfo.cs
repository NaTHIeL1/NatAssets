using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class BundleInfo : BaseInfo
    {
        internal AssetBundle Bundle;
        //private string[] _dependBundles = null;
        internal static BundleInfo CreateBundleInfo(string bundlePath, AssetBundle bundle)
        {
            BundleInfo bundleInfo = Create<BundleInfo>(bundlePath);
            bundleInfo.CreateInfo(bundle);
            return bundleInfo;
        }

        private void CreateInfo(AssetBundle bundle)
        {
            Bundle = bundle;
        }

        protected override void OnAddRefCount()
        {
            if (RefCount == 0)
            {
                CancelUnLoadTask();
            }
        }

        protected override void OnRedRefCount()
        {
            if (CheckNeedUnLoadInfo())
            {
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
            _unLoadTask = BundleUnLoadTask.CreateTask<BundleUnLoadTask>(InfoNameGUID, Priority.Low);
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
            _unLoadTask = null;
            Bundle = null;
        }

        public void Clear()
        {
            
        }
    }
}