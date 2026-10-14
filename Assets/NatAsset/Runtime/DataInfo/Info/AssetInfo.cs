using System;
using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class AssetInfo : BaseInfo
    {
        internal object Asset { get; private set; }
        internal List<string> DepBundles { get; private set; }
        internal Type AssetType { get; private set; }
        internal AssetLifetimeInfo Lifetime;

        internal static AssetInfo CreateAssetInfo(string path, Type type, object asset, List<string> bundles)
        {
            AssetInfo info = Create<AssetInfo>(RuntimeData.NormalizeAssetPath(path));
            info.Asset = asset;
            info.AssetType = type;
            info.DepBundles = bundles;
            return info;
        }

        protected override void OnAddRefCount() => RuntimeData.RefreshAssetReferences(Lifetime);
        protected override void OnRedRefCount() => RuntimeData.RefreshAssetReferences(Lifetime);
        protected override void OnLock() => RuntimeData.RefreshAssetReferences(Lifetime);
        protected override void OnUnLock() => RuntimeData.RefreshAssetReferences(Lifetime);

        protected override void OnClear()
        {
            Asset = null;
            AssetType = null;
            DepBundles = null;
            Lifetime = null;
        }
    }
}