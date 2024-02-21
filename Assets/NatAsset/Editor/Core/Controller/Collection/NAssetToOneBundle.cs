using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Editor
{
    public class NAssetToOneBundle : IBuildCollector
    {
        private PackageVirtualRoot _packageVirtualRoot;
        public List<BundleBuildInfo> OnCollection(PackageVirtualRoot packageRoot)
        {
            //强制刷新
            packageRoot.ForceBuild();
            _packageVirtualRoot = packageRoot;
            if (_packageVirtualRoot.Child == null) return null;
            List<BundleBuildInfo> bundleBuildInfos = new List<BundleBuildInfo>();
            BundleBuildInfo buildInfo = BundleBuildInfo.CreateByIVirtualFile(_packageVirtualRoot, _packageVirtualRoot.Group, _packageVirtualRoot.EncryptName);
            bundleBuildInfos.Add(buildInfo);
            AddDependAsset(buildInfo, _packageVirtualRoot);
            return bundleBuildInfos;
        }

        private void AddDependAsset(BundleBuildInfo buildInfo, IVirtualFile virtualFile)
        {
            //文件分包逻辑
            BuildAssetFile(virtualFile, buildInfo);
            
            //文件夹分包逻辑
            if(virtualFile.Child == null) return;
            for (int i = 0; i < virtualFile.Child.Count; i++)
            {
                IVirtualFile tempVirtualFile = virtualFile.Child[i];
                BuildAssetFile(tempVirtualFile, buildInfo);
                if (tempVirtualFile is VirtualFolder)
                {
                    AddDependAsset(buildInfo, tempVirtualFile);
                }
            }
        }

        private void BuildAssetFile(IVirtualFile virtualFile, BundleBuildInfo buildInfo)
        {
            if (virtualFile is VirtualFile && NatAssetEditorUtil.IsValidAsset(virtualFile.FileName))
            {
                AssetBuildInfo assetBuildInfo = AssetBuildInfo.CreateByIVirtualFile(virtualFile, buildInfo);
                buildInfo.AssetBuildInfos.Add(assetBuildInfo);
            }
        }
    }
}