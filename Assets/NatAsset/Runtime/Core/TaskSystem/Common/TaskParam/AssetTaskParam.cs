using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal struct AssetTaskParam : ITaskParam
    {
        public string TaskGUID => _taskGUID;
        public string TaskName => _taskName;
        public Type AssetType => _type;

        private string _taskGUID;
        private string _taskName;
        private Type _type;

        public AssetTaskParam(string taskName, Type assetType)
        {
            _taskGUID = TypeStringGUIDPool.GetTargetGUID(assetType, taskName);
            _taskName = taskName;
            _type = assetType;
        }
    }
}
