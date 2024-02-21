using System;
using System.Collections.Generic;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public class BundleBuildInfo
    {
        public string EditorPath;
        public UnityEngine.Object DirectoryObj;

        /// <summary>
        /// 原BundlePath,含大小写
        /// </summary>
        public string CompletePath;

        /// <summary>
        /// BundlePath
        /// </summary>
        public string BundlePath;

        public string BundleGroup = "Base";

        public string MD5;

        public EditorBundleEncrypt BundleEncrypt;

        public List<AssetBuildInfo> AssetBuildInfos = new List<AssetBuildInfo>();

        public long Length;

        public BundleBuildInfo(string editorPath, string completePath, string group, EditorBundleEncrypt bundleEncrypt)
        {
            EditorPath = editorPath;
            CompletePath = completePath;
            BundleGroup = group;
            BundleEncrypt = bundleEncrypt;
            //生成bundle路径
            BundlePath = NatAssetEditorUtil.FormatBundlePath(CompletePath);
        }

        public AssetBundleBuild GetAssetBundleBuild()
        {
            AssetBundleBuild assetBundleBuild = new AssetBundleBuild();
            assetBundleBuild.assetBundleName = BundlePath;
            //AssetBuildInfos.Sort();
            string[] assetNames = new string[AssetBuildInfos.Count];
            for (int i = 0; i < AssetBuildInfos.Count; i++)
            {
                assetNames[i] = AssetBuildInfos[i].EditorPath;
            }

            assetBundleBuild.assetNames = assetNames;
            return assetBundleBuild;
        }

        public void RefreshAllLength()
        {
            Length = 0;
            for (int i = 0; i < AssetBuildInfos.Count; i++)
            {
                Length += AssetBuildInfos[i].Length;
            }
        }

        internal static BundleBuildInfo CreateByPackageRoot(PackageVirtualRoot virtualRoot)
        {
            BundleBuildInfo buildInfo = new BundleBuildInfo(virtualRoot.FileName,
                virtualRoot.FullName.Replace(Application.dataPath + "/", ""), virtualRoot.Group, virtualRoot.EncryptName);
            return buildInfo;
        }

        internal static BundleBuildInfo CreateByIVirtualFile(IVirtualFile virtualFile, string group, EditorBundleEncrypt EncryptName)
        {
            BundleBuildInfo buildInfo = new BundleBuildInfo(virtualFile.FileName,
                virtualFile.FullName.Replace(Application.dataPath + "/", ""), group, EncryptName);
            return buildInfo;
        }
    }
}