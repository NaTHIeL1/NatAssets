using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetExtend
    {
        public static AssetHandle OnBind(this AssetHandle assetHandle, GameObject gameObject)
        {
            //当物体销毁时触发handle卸载逻辑
            if (gameObject == null)
                throw new Exception("不能绑定空物体");
            NatAssetsBind assetBind = gameObject.GetComponent<NatAssetsBind>();
            if (assetBind == null)
                assetBind = gameObject.AddComponent<NatAssetsBind>();
            assetBind.BindHandle(assetHandle);
            return assetHandle;
        }
    }
}