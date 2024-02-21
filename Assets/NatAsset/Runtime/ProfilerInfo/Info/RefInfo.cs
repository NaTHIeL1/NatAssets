using System;

#if UNITY_EDITOR
namespace NATFrameWork.Profiler
{
    [Obsolete("供编辑器使用")]
    public struct RefInfo
    {
        public NatAssetType NatAssetType;
        public string InfoName;
        public int RefCount;

        public void SetInfo(NatAssetType natAssetType, string infoName, int refCount)
        {
            this.NatAssetType = natAssetType;
            this.InfoName = infoName;
            this.RefCount = refCount;
        }
    }

    [Obsolete("供编辑器使用")]
    public enum NatAssetType
    {
        Asset,
        Bundle,
        Scene,
    }
}
#endif
