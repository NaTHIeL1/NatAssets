using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class NatAssetsBind : MonoBehaviour
    {
        private BindHandleList bindHandleList = null;
        private void Awake()
        {
            bindHandleList = ReferencePool.Get<BindHandleList>();
        }

        private void OnDestroy()
        {
            if(bindHandleList != null)
            {
                ReferencePool.Release(bindHandleList);
                bindHandleList = null;
            }
        }

        internal void BindHandle(AssetHandle assetHandle)
        {
            if (assetHandle == null)
                return;
            if(bindHandleList != null)
            {
                bindHandleList.BindHandle(assetHandle);
            }
        }
    }
}
