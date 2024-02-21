#if !NATASSET_SBP_SUPPORT
using System;
using System.IO;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static partial class RuntimeData
    {

        private static AssetBundleManifest _manifest = null;
        internal static AssetBundleManifest Manifest => _manifest;


        //internal static void LoadMainfest()
        //{
        //    BundleInfo bundleInfo = GetBundle(NatAssetSetting.ConfigName);
        //    if (bundleInfo != null) return;
        //    AssetBundle mainAssertBundle =
        //        AssetBundle.LoadFromFile(Path.Combine(NatAssetDataSetting.LoadPath,
        //            NatAssetConfig["Config"]["Platform"]));
        //    if (mainAssertBundle == null)
        //    {
        //        throw new Exception("mainAssetBundle(主包清单)加载失败");
        //    }

        //    _manifest = mainAssertBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        //    bundleInfo = BundleInfo.CreateBundleInfo(NatAssetDataSetting.MainfestName, mainAssertBundle);
        //    bundleInfo.AddRefCount();
        //    AddBundleInfo(bundleInfo);
        //}

        //internal static void UnLoadMainfest()
        //{
        //    BundleInfo bundleInfo = GetBundle(NatAssetDataSetting.MainfestName);
        //    if (bundleInfo != null)
        //    {
        //        _manifest = null;
        //        RemoveBundleInfo(bundleInfo);
        //        bundleInfo.Bundle.Unload(true);
        //    }
        //}
    }
}
#endif