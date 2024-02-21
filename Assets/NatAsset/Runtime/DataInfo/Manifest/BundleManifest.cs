using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NATFrameWork.NatAsset.Runtime
{
    [Serializable]
    public class BundleManifest
    {
        public string BundlePath;
        //资源组
        public string Group;
        public string MD5;
        public string Hash;
        public uint CRC;
        //是否为原生资源
        public bool IsRaw;
        public bool IsAppendHash;
        public ulong Length;

        //该字段不存配置文件
        internal LoadPath LoadPath;

        public BundleEncrypt BundleEncrypt;
        //依赖Bundle
        public string[] Dependencies;
        //包含资源
        public List<AssetManifest> Assets;
        
        public void SerializeToBinary(BinaryWriter bw)
        {
            bw.Write(BundlePath);
            bw.Write(Group);
            bw.Write(MD5);
            bw.Write(Hash);
            bw.Write(CRC);
            bw.Write(IsRaw);
            bw.Write(IsAppendHash);
            bw.Write(Length);
            bw.Write((byte)BundleEncrypt);
            bw.Write(Dependencies.Length);
            for (int i = 0; i < Dependencies.Length; i++)
            {
                bw.Write(Dependencies[i]);
            }

            bw.Write(Assets.Count);
            for (int i = 0; i < Assets.Count; i++)
            {
                Assets[i].SerializeToBinary(bw);
            }
        }

        public static BundleManifest DeserializeFromBinary(BinaryReader br)
        {
            BundleManifest bundleManifest = new BundleManifest();
            bundleManifest.BundlePath = br.ReadString();
            bundleManifest.Group = br.ReadString();
            bundleManifest.MD5 = br.ReadString();
            bundleManifest.Hash = br.ReadString();
            bundleManifest.CRC = br.ReadUInt32();
            bundleManifest.IsRaw = br.ReadBoolean();
            bundleManifest.IsAppendHash = br.ReadBoolean();
            bundleManifest.Length = br.ReadUInt64();
            bundleManifest.BundleEncrypt = (BundleEncrypt)br.ReadByte();
            int count = br.ReadInt32();
            bundleManifest.Dependencies = new string[count];
            for (int i = 0; i < count; i++)
            {
                string str = br.ReadString();
                bundleManifest.Dependencies[i] = str;
            }

            int assetCount = br.ReadInt32();
            bundleManifest.Assets = new List<AssetManifest>(assetCount);
            for (int i = 0; i < assetCount; i++)
            {
                AssetManifest assetManifest = AssetManifest.DeserializeFromBinary(br);
                bundleManifest.Assets.Add(assetManifest);
            }
            return bundleManifest;
        }
    }
}
