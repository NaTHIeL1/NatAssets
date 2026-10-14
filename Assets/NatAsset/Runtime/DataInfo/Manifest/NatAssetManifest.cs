using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    [Serializable]
    public class NatAssetManifest
    {
        public int NatAssetVersion = NatAssetSetting.NatAssetVersion;
        //public int Version;
        //todo:完善版本号机制，版本号分为构建版本和发布版本
        public string BuildVersion;
        public string ReleaseVersion;
        public string Platform;
        public List<string> IncludeGroups;
        public List<BundleManifest> BundleManifests;
        [NonSerialized]
        public Dictionary<string, AssetManifest> AssetManifestDic;
        [NonSerialized]
        public Dictionary<string, BundleManifest> BundleManifestDic;

        internal void InitRunTimeManifest(LoadPath loadPath)
        {
            AssetManifestDic = new Dictionary<string, AssetManifest>(1000);
            BundleManifestDic = new Dictionary<string, BundleManifest>(1000);

            List<BundleManifest> bundleList = BundleManifests;
            if (bundleList != null)
            {
                foreach (BundleManifest bundleManifest in bundleList)
                {
                    bundleManifest.LoadPath = loadPath;
                    BundleManifestDic.Add(bundleManifest.BundlePath, bundleManifest);

                    List<AssetManifest> assetList = bundleManifest.Assets;
                    foreach (AssetManifest assetManifest in assetList)
                    {
                        assetManifest.BundleName = bundleManifest.BundlePath;
                        AssetManifestDic.Add(assetManifest.AssetName, assetManifest);
                    }
                }
            }
        }

        public BundleManifest GetBundleManifest(string bundlePath)
        {
            if (string.IsNullOrEmpty(bundlePath)) return null;
            if (BundleManifestDic == null) return null;
            if(BundleManifestDic.ContainsKey(bundlePath))
                return BundleManifestDic[bundlePath];
            return null;
        }

        public AssetManifest GetAssetManifest(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (AssetManifestDic == null) return null;
            if (AssetManifestDic.ContainsKey(assetPath))
                return AssetManifestDic[assetPath];
            return null;
        }

        public Byte[] SerializeToBinary()
        {
            ValidateDependencies();
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(NatAssetVersion);
                    bw.Write(BuildVersion);
                    bw.Write(ReleaseVersion);
                    bw.Write(Platform);
                    bw.Write(IncludeGroups.Count);
                    for(int i = 0; i < IncludeGroups.Count; i++)
                    {
                        bw.Write(IncludeGroups[i]);
                    }
                    bw.Write(BundleManifests.Count);
                    for (int i = 0; i < BundleManifests.Count; i++)
                    {
                        BundleManifests[i].SerializeToBinary(bw);
                    }

                    Byte[] bytes = ms.ToArray();
                    return bytes;
                }
            }
        }

        public static NatAssetManifest DeserializeFromBinary(Byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                throw new InvalidDataException("资源清单数据为空。");
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    NatAssetManifest natAssetManifest = new NatAssetManifest();
                    natAssetManifest.BundleManifests = new List<BundleManifest>();
                    natAssetManifest.IncludeGroups = new List<string>();
                    natAssetManifest.NatAssetVersion = br.ReadInt32();
                    if (natAssetManifest.NatAssetVersion != NatAssetSetting.NatAssetVersion)
                    {
                        throw new InvalidDataException($"NatAsset资源清单格式不匹配（{natAssetManifest.NatAssetVersion} / {NatAssetSetting.NatAssetVersion}），需要重新生成资源清单。");
                    }
                    natAssetManifest.BuildVersion = br.ReadString();
                    natAssetManifest.ReleaseVersion = br.ReadString();
                    natAssetManifest.Platform = br.ReadString();
                    int groupCount = br.ReadInt32();
                    for(int i = 0; i < groupCount; i++)
                    {
                        natAssetManifest.IncludeGroups.Add(br.ReadString());
                    }
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        natAssetManifest.BundleManifests.Add(BundleManifest.DeserializeFromBinary(br));
                    }
                    natAssetManifest.ValidateDependencies();

                    return natAssetManifest;
                }
            }
        }
        private void ValidateDependencies()
        {
            var assets = new Dictionary<string, AssetManifest>(StringComparer.Ordinal);
            var bundles = new HashSet<string>(StringComparer.Ordinal);
            foreach (BundleManifest bundle in BundleManifests)
            {
                if (!bundles.Add(bundle.BundlePath)) throw new InvalidDataException("重复 Bundle: " + bundle.BundlePath);
                foreach (AssetManifest asset in bundle.Assets)
                {
                    if (string.IsNullOrEmpty(asset.AssetName) || asset.AssetName.Contains("\\") || assets.ContainsKey(asset.AssetName))
                        throw new InvalidDataException("无效或重复资源路径: " + asset.AssetName);
                    assets.Add(asset.AssetName, asset);
                }
            }
            foreach (BundleManifest bundle in BundleManifests)
            {
                foreach (string dependency in bundle.Dependencies)
                    if (!bundles.Contains(dependency)) throw new InvalidDataException("Bundle依赖未登记: " + dependency);
                foreach (AssetManifest asset in bundle.Assets)
                {
                    if (asset.Dependencies == null) throw new InvalidDataException("资源缺少依赖列表: " + asset.AssetName);
                    if (!Enum.IsDefined(typeof(AssetUnloadPolicy), asset.UnloadPolicy)) throw new InvalidDataException("资源卸载策略无效: " + asset.AssetName);
                    var unique = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string dependency in asset.Dependencies)
                        if (dependency == asset.AssetName || !unique.Add(dependency) || !assets.ContainsKey(dependency))
                            throw new InvalidDataException($"无效资源依赖: {asset.AssetName} -> {dependency}");
                }
            }
        }
    }
}
