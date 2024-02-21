using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetExtend
    {
        public static AssetHandle OnBind(this AssetHandle assetHandle, GameObject gameObject)
        {
            //当物体销毁时触发handle卸载逻辑
            //使用unitask
            if (gameObject == null)
                throw new Exception("不能绑定空物体");
            gameObject.OnDestroyAsync().ContinueWith(assetHandle.Unload);
            return assetHandle;
        }
    }
}