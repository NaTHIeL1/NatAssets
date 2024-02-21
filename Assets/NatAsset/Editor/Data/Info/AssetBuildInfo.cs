using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public class AssetBuildInfo : IComparable<AssetBuildInfo>, IEquatable<AssetBuildInfo>
    {
        public UnityEngine.Object DirectoryObj;
        public string EditorPath;
        public string BundlePath;
        public string AssetName;
        public long Length;

        public AssetBuildInfo(string editorPath, string bundlePath, string assetName)
        {
            EditorPath = editorPath;
            BundlePath = bundlePath;
            AssetName = assetName;
            Length = new FileInfo(EditorPath).Length;
        }

        public Type Type
        {
            get
            {
                return AssetDatabase.GetMainAssetTypeAtPath(EditorPath);
            }
        }

        public static AssetBuildInfo CreateByIVirtualFile(IVirtualFile virtualFile, BundleBuildInfo buildInfo)
        {
            AssetBuildInfo assetBuildInfo = new AssetBuildInfo(virtualFile.FileName, buildInfo.BundlePath,
                virtualFile.Name);
            return assetBuildInfo;
        }

        public int CompareTo(AssetBuildInfo other)
        {
            return EditorPath.CompareTo(other.EditorPath);
        }

        public bool Equals(AssetBuildInfo other)
        {
            return EditorPath.Equals(other.EditorPath);
        }

        public override int GetHashCode()
        {
            return EditorPath.GetHashCode();
        }
    }
}