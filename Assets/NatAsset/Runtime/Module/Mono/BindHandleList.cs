using System;
using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class BindHandleList : IReference
    {
        private List<AssetHandle> assetHandles = null;
        public void Clear()
        {
            if (assetHandles != null)
            {
                for (int i = 0; i < assetHandles.Count; i++)
                {
                    assetHandles[i].Dispose();
                }
                assetHandles.Clear();
            }
        }
        internal void BindHandle(AssetHandle assetHandle)
        {
            if (assetHandle == null)
                return;
            if (assetHandles == null)
                assetHandles = new List<AssetHandle>();

            assetHandles.Add(assetHandle);
        }
    }
}
