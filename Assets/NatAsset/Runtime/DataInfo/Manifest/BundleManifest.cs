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
        public string MainGroup;
        public List<string> Groups;
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

        private string _relativePath = string.Empty;

        /// <summary>
        /// 用于加载时的实际路径
        /// </summary>
        public string RelativePath
        {
            get
            {
                if (string.IsNullOrEmpty(_relativePath))
                {
                    if(IsAppendHash)
                    {
                        string[] nameArray = BundlePath.Split('.');
                        string hashBundleName = $"{nameArray[0]}_{Hash}.{nameArray[1]}";
                        _relativePath = hashBundleName;
                    }
                    else
                    {
                        _relativePath = BundlePath;
                    }
                }
                return _relativePath;
            }
        }

        /// <summary>
        /// 检查AB是否一致
        /// </summary>
        /// <param name="remoteManifest"></param>
        /// <returns></returns>
        public bool EquipABVersion(BundleManifest remoteManifest)
        {
            if(remoteManifest != null)
            {
                if (BundlePath.Equals(remoteManifest.BundlePath) 
                    && Hash.Equals(remoteManifest.Hash)
                    && Length == remoteManifest.Length
                    && MD5.Equals(remoteManifest.MD5))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 由于获取的bundle的全部依赖，可能依赖的bundle产生依赖变化，所以保险起见检查依赖是否需要更新
        /// </summary>
        /// <param name="remoteManifest"></param>
        /// <returns></returns>
        public bool NeedUpdateManifest(BundleManifest remoteManifest)
        {
            if(remoteManifest != null && BundlePath.Equals(remoteManifest.BundlePath))
            {
                if (!MainGroup.Equals(remoteManifest.MainGroup))
                    return true;
                if(Groups.Count != remoteManifest.Groups.Count)
                    return true;
                if (Dependencies.Length != remoteManifest.Dependencies.Length)
                    return true;
                for (int i = 0; i < Groups.Count; i++)
                {
                    if (!Groups[i].Equals(remoteManifest.Groups[i]))
                    {
                        return true;
                    }
                }
                for (int i = 0; i < Dependencies.Length; i++)
                {
                    if (!Dependencies[i].Equals(remoteManifest.Dependencies[i]))
                        return true;
                }
            }
            return false;
        }
        
        public void SerializeToBinary(BinaryWriter bw)
        {
            bw.Write(BundlePath);
            bw.Write(MainGroup);

            bw.Write(Groups.Count);
            for(int i = 0; i < Groups.Count; i++)
            {
                bw.Write(Groups[i]);
            }

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
            bundleManifest.MainGroup = br.ReadString();

            int groupCount = br.ReadInt32();
            bundleManifest.Groups = new List<string>(groupCount);
            for (int i = 0; i < groupCount; i++)
            {
                bundleManifest.Groups.Add(br.ReadString());
            }

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
