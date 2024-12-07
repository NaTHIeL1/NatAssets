using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal struct BundleDownLoadTaskParam : ITaskParam
    {
        public string TaskGUID => _taskGUID;

        public string TaskName => _taskName;
        public BundleManifest BundleManifest => _bundleManifest;

        private string _taskGUID;
        private string _taskName;
        private BundleManifest _bundleManifest;

        public BundleDownLoadTaskParam(string taskGUID, string taskName, BundleManifest bundleManifest)
        {
            _taskGUID = taskGUID;
            _taskName = taskName;
            _bundleManifest = bundleManifest;
        }
    }
}
