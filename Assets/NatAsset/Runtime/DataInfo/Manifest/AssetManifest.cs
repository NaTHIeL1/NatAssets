using System;
using System.IO;
using System.Text;

namespace NATFrameWork.NatAsset.Runtime
{
    [Serializable]
    public class AssetManifest
    {
        //public string TitleName;
        public string AssetName;
        [NonSerialized]
        public string BundleName;
        public void SerializeToBinary(BinaryWriter bw)
        {
            bw.Write(AssetName);
        }

        public static AssetManifest DeserializeFromBinary(BinaryReader br)
        {
            AssetManifest assetManifest = new AssetManifest();
            assetManifest.AssetName = br.ReadString();
            return assetManifest;
        }
    }
}
