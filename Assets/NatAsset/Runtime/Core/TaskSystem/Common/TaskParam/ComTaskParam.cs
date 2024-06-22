using System;

namespace NATFrameWork.NatAsset.Runtime
{
    internal struct ComTaskParam : ITaskParam
    {
        public string TaskGUID => _taskGUID;

        public string TaskName => _taskName;

        private string _taskGUID;
        private string _taskName;

        public ComTaskParam(string taskGUID, string taskName)
        {
            _taskGUID = taskGUID;
            _taskName = taskName;
        }
    }
}
