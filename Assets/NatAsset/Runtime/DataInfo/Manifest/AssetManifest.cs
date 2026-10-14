using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace NATFrameWork.NatAsset.Runtime
{
    [Serializable]
    public class AssetManifest
    {
        //public string TitleName;
        public string AssetName;
        public List<string> Dependencies = new List<string>();
        public bool IsAtlasPackable;
        public AssetUnloadPolicy UnloadPolicy;
        [NonSerialized]
        public string BundleName;
        public void SerializeToBinary(BinaryWriter bw)
        {
            bw.Write(AssetName);
            bw.Write(IsAtlasPackable);
            bw.Write((int)UnloadPolicy);
            bw.Write(Dependencies.Count);
            foreach (string dependency in Dependencies) bw.Write(dependency);
        }

        public static AssetManifest DeserializeFromBinary(BinaryReader br)
        {
            AssetManifest assetManifest = new AssetManifest();
            assetManifest.AssetName = br.ReadString();
            assetManifest.IsAtlasPackable = br.ReadBoolean();
            assetManifest.UnloadPolicy = (AssetUnloadPolicy)br.ReadInt32();
            if (!Enum.IsDefined(typeof(AssetUnloadPolicy), assetManifest.UnloadPolicy))
                throw new InvalidDataException("Invalid asset unload policy: " + assetManifest.AssetName);
            int count = br.ReadInt32();
            if (count < 0 || count > 1000000) throw new InvalidDataException("Invalid dependency count.");
            for (int i = 0; i < count; i++) assetManifest.Dependencies.Add(br.ReadString());
            return assetManifest;
        }
    }

    public enum AssetUnloadPolicy
    {
        BundleOnly = 0,
        Independent = 1
    }
}
