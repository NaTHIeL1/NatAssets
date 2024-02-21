using System;

#if UNITY_EDITOR
namespace NATFrameWork.Profiler
{
    [Obsolete("供编辑器使用")]
    public struct HandleInfo
    {
        public NatAssetType HandleType;
        public string HandleName;
        public string StackRecord;

        public void SetParam(string handleName, string stackRecord, NatAssetType handleType)
        {
            this.HandleName = handleName;
            this.StackRecord = stackRecord;
            this.HandleType = handleType;
        }
    }
}
#endif
