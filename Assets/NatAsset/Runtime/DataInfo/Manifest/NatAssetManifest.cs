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
                    if (!BundleManifestDic.ContainsKey(bundleManifest.BundlePath))
                        BundleManifestDic.Add(bundleManifest.BundlePath, bundleManifest);

                    List<AssetManifest> assetList = bundleManifest.Assets;
                    foreach (AssetManifest assetManifest in assetList)
                    {
                        assetManifest.BundleName = bundleManifest.BundlePath;
                        if (!AssetManifestDic.ContainsKey(assetManifest.AssetName))
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
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(NatAssetVersion);
                    bw.Write(BuildVersion);
                    bw.Write(ReleaseVersion);
                    bw.Write(Platform);
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
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    NatAssetManifest natAssetManifest = new NatAssetManifest();
                    natAssetManifest.BundleManifests = new List<BundleManifest>();
                    natAssetManifest.NatAssetVersion = br.ReadInt32();
                    natAssetManifest.BuildVersion = br.ReadString();
                    natAssetManifest.ReleaseVersion = br.ReadString();
                    natAssetManifest.Platform = br.ReadString();
                    if (natAssetManifest.NatAssetVersion < NatAssetSetting.NatAssetVersion)
                    {
                        Debug.LogWarning(
                            $"NatAsset配置文件版本号{natAssetManifest.NatAssetVersion}无法匹配当前代码版本{NatAssetSetting.NatAssetVersion}");
                        return null;
                    }

                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        natAssetManifest.BundleManifests.Add(BundleManifest.DeserializeFromBinary(br));
                    }

                    return natAssetManifest;
                }
            }
        }
    }
}