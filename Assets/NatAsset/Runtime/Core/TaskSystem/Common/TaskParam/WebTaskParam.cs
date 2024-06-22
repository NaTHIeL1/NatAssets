using System;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public struct WebTaskParam : ITaskParam
    {
        public string TaskGUID => _taskGUID;
        public string TaskName => _taskName;
        public int RetryCount => _retryCount;
        public Action<string, bool, UnityWebRequest> CallBack => _callback;

        private string _taskGUID;
        private string _taskName;
        private int _retryCount;
        private Action<string, bool, UnityWebRequest> _callback;

        public WebTaskParam(string taskGUID, string taskName, int retryCount, Action<string, bool, UnityWebRequest> callback)
        {
            this._taskGUID = taskGUID;
            this._taskName = taskName;
            this._retryCount = retryCount;
            this._callback = callback;
        }
    }
}
