using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Editor
{
    public class NAssetToNBundle : IBuildCollector
    {
        private PackageVirtualRoot _packageVirtualRoot;

        public List<BundleBuildInfo> OnCollection(PackageVirtualRoot packageRoot)
        {
            //强制刷新
            packageRoot.ForceBuild();
            _packageVirtualRoot = packageRoot;
            if (packageRoot.Child == null) return null;
            List<BundleBuildInfo> bundleBuildInfos = new List<BundleBuildInfo>();
            AddDependAsset(bundleBuildInfos, packageRoot);
            return bundleBuildInfos;
        }

        private void AddDependAsset(List<BundleBuildInfo> buildInfos, IVirtualFile virtualFile)
        {
            //文件分包逻辑
            if (virtualFile is VirtualFile && NatAssetEditorUtil.IsValidAsset(virtualFile.FileName))
            {
                //构建Bundle
                BundleBuildInfo buildInfo = BundleBuildInfo.CreateByIVirtualFile(virtualFile,
                    _packageVirtualRoot.Group, _packageVirtualRoot.EncryptName);
                AssetBuildInfo assetBuildInfo = AssetBuildInfo.CreateByIVirtualFile(virtualFile, buildInfo);
                buildInfo.AssetBuildInfos.Add(assetBuildInfo);
                //加入收集清单
                buildInfos.Add(buildInfo);
            }
            
            //文件夹分包逻辑
            if (virtualFile.Child == null) return;
            for (int i = 0; i < virtualFile.Child.Count; i++)
            {
                IVirtualFile tempVirtualFile = virtualFile.Child[i];
                if (tempVirtualFile is VirtualFile && NatAssetEditorUtil.IsValidAsset(tempVirtualFile.FileName))
                {
                    //构建Bundle
                    BundleBuildInfo buildInfo = BundleBuildInfo.CreateByIVirtualFile(tempVirtualFile,
                        _packageVirtualRoot.Group, _packageVirtualRoot.EncryptName);
                    AssetBuildInfo assetBuildInfo = AssetBuildInfo.CreateByIVirtualFile(tempVirtualFile, buildInfo);
                    buildInfo.AssetBuildInfos.Add(assetBuildInfo);
                    //加入收集清单
                    buildInfos.Add(buildInfo);
                }
                else if (tempVirtualFile is VirtualFolder)
                {
                    AddDependAsset(buildInfos, tempVirtualFile);
                }
            }
        }
    }
}