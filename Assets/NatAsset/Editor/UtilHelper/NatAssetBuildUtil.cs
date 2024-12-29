using System.Collections.Generic;
using System.IO;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public static class NatAssetBuildUtil
    {
        /// <summary>
        /// 构建bundle资源
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="buildVersion"></param>
        public static void BuildAssetBundles(ValidBuildTarget buildTarget, VersionData buildVersion = default)
        {
            if (buildVersion == default)
                buildVersion = VersionData.NowVersion();
            NatBuildParamters natBuildParamters = new NatBuildParamters(buildTarget, CollectBundles());
            natBuildParamters.VersionData = buildVersion;
            NatBuildConfig.Build(natBuildParamters);
        }

        /// <summary>
        /// 收集Bundle资源
        /// </summary>
        /// <returns></returns>
        public static List<BundleBuildInfo> CollectBundles()
        {
            EditorUtility.DisplayProgressBar("AnaylzeBundle", "In the analysis phase", 0.2f);
            List<PackageVirtualRoot> packageRoots = VirtualFileUtil.VirtualFiles;
            if (packageRoots == null || packageRoots.Count == 0)
            {
                Debug.LogError("未构建配置");
                return null;
            }

            List<BundleBuildInfo> buildInfos = new List<BundleBuildInfo>();
            for (int i = 0; i < packageRoots.Count; i++)
            {
                PackageVirtualRoot temp = packageRoots[i];
                IBuildCollector buildCollector = NatAssetEditorUtil.BundleCollectorDic[temp.CollectorName];
                List<BundleBuildInfo> tempBuildInfos = buildCollector.OnCollection(temp);
                if (tempBuildInfos == null || tempBuildInfos.Count == 0) continue;
                for (int j = 0; j < tempBuildInfos.Count; j++)
                {
                    buildInfos.Add(tempBuildInfos[j]);
                }
            }
            //分析隐式依赖，将隐式依赖转为显式依赖
            buildInfos = AnlysizeImplicitAssetToExplicit(buildInfos);

            //分析冗余资源
            Dictionary<string, List<BundleBuildInfo>> redundantDic = AnalyzeRedundantAssets(buildInfos);

            //并构建额外的冗余资源包
            List<BundleBuildInfo> sharedBundle = AnaylzeImplicitAssetToBundleBuild(redundantDic);

            //将共享Bundle添加至总Bundle列表中
            buildInfos = AddSharedBundle(buildInfos, sharedBundle);

            //分割场景资源和普通资源
            List<BundleBuildInfo> results = SplitSceneBundle(buildInfos);
            
            //计算BundleSize
            AnaylzeBundleAssetsSize(results);
            
            EditorUtility.ClearProgressBar();

            Debug.Log("AssetBundle分析完毕");
            return results;
        }

        /// <summary>
        /// 转换NatAsset的Bundle收集格式至UnityEditor.AssetBundleBuild格式
        /// </summary>
        /// <param name="buildInfos"></param>
        /// <returns></returns>
        public static AssetBundleBuild[] SwitchBBIsToABBs(List<BundleBuildInfo> buildInfos)
        {
            if (buildInfos == null) return null;
            List<AssetBundleBuild> assetBundleBuilds = new List<AssetBundleBuild>();
            for (int i = 0; i < buildInfos.Count; i++)
            {
                assetBundleBuilds.Add(buildInfos[i].GetAssetBundleBuild());
            }

            return assetBundleBuilds.ToArray();
        }

        /// <summary>
        /// 将隐式资源转换为显式资源
        /// </summary>
        /// <param name="bundleBuildInfos"></param>
        private static List<BundleBuildInfo> AnlysizeImplicitAssetToExplicit(List<BundleBuildInfo> bundleBuildInfos)
        {
            //填充显式资源
            HashSet<string> explicitAssets = new HashSet<string>();
            foreach (BundleBuildInfo info in bundleBuildInfos)
            {
                foreach (AssetBuildInfo assetBuildInfo in info.AssetBuildInfos)
                {
                    explicitAssets.Add(assetBuildInfo.EditorPath);
                }
            }

            //分析隐式资源并转换为显式依赖
            foreach (BundleBuildInfo bundleBuildInfo in bundleBuildInfos)
            {
                HashSet<string> implicitAssets = new HashSet<string>();
                foreach (AssetBuildInfo assetBuildInfoIn in bundleBuildInfo.AssetBuildInfos)
                {
                    List<string> dependenceAssets = NatAssetEditorUtil.GetDependencies(assetBuildInfoIn.EditorPath);
                    foreach (string dependenceAsset in dependenceAssets)
                    {
                        if (!explicitAssets.Contains(dependenceAsset))
                        {
                            implicitAssets.Add(dependenceAsset);
                        }
                    }
                }

                foreach (string assetEditorName in implicitAssets)
                {
                    string assetName = Path.GetFileName(assetEditorName);
                    bundleBuildInfo.AssetBuildInfos.Add(new AssetBuildInfo(assetEditorName, bundleBuildInfo.BundlePath, assetName));
                }
            }
            return bundleBuildInfos;
        }

        /// <summary>
        /// 分析冗余资源，找出需要处理的冗余资源（从隐式资源转显式资源）
        /// </summary>
        /// <param name="bundleBuildInfos"></param>
        private static Dictionary<string, List<BundleBuildInfo>> AnalyzeRedundantAssets(List<BundleBuildInfo> bundleBuildInfos)
        {
            Dictionary<string, List<BundleBuildInfo>> assetDependBundleDic = new Dictionary<string, List<BundleBuildInfo>>();

            foreach (BundleBuildInfo bundleBuildInfo in bundleBuildInfos)
            {
                foreach (AssetBuildInfo assetInfo in bundleBuildInfo.AssetBuildInfos)
                {
                    if (!assetDependBundleDic.TryGetValue(assetInfo.EditorPath, out List<BundleBuildInfo> list))
                    {
                        list = new List<BundleBuildInfo>();
                        assetDependBundleDic.Add(assetInfo.EditorPath, list);
                    }
                    list.Add(bundleBuildInfo);
                }
            }

            Dictionary<string, List<BundleBuildInfo>> assetBundleBuildDic = new Dictionary<string, List<BundleBuildInfo>>();
            foreach (var keyValuepair in assetDependBundleDic)
            {
                if (keyValuepair.Value.Count >= 2)
                {
                    foreach (BundleBuildInfo bundleBuildInfo in keyValuepair.Value)
                    {
                        if (!assetBundleBuildDic.TryGetValue(keyValuepair.Key, out List<BundleBuildInfo> list))
                        {
                            list = new List<BundleBuildInfo>();
                            assetBundleBuildDic.Add(keyValuepair.Key, list);
                        }
                        list.Add(bundleBuildInfo);

                        //移除被多个bundle所依赖的资源
                        List<AssetBuildInfo> assetInfos = bundleBuildInfo.AssetBuildInfos;
                        for (int i = 0; i < assetInfos.Count; i++)
                        {
                            AssetBuildInfo temp = assetInfos[i];
                            if (temp.EditorPath == keyValuepair.Key)
                            {
                                bundleBuildInfo.AssetBuildInfos.Remove(temp);
                                break;
                            }
                        }
                    }
                }
            }
            return assetBundleBuildDic;
        }

        /// <summary>
        /// 分析这些隐式资源之间的依赖关系，找出全部根资源
        /// </summary>
        /// <param name="originDic"></param>
        /// <returns></returns>
        private static List<BundleBuildInfo> AnaylzeImplicitAssetToBundleBuild(Dictionary<string, List<BundleBuildInfo>> originDic)
        {
            Dictionary<string, int> assetRefCount = new Dictionary<string, int>();

            void AddRef(string assetName)
            {
                if (!assetRefCount.TryGetValue(assetName, out int refCount))
                {
                    assetRefCount.Add(assetName, 0);
                }
                assetRefCount[assetName]++;
            }

            foreach (var keyValuepairs in originDic)
            {
                AddRef(keyValuepairs.Key);
                List<string> assetNames = NatAssetEditorUtil.GetDependencies(keyValuepairs.Key);
                foreach (string assetName in assetNames)
                {
                    AddRef(assetName);
                }
            }

            //获取资源根节点
            List<string> rootAsset = new List<string>();
            foreach (var keyValuepair in assetRefCount)
            {
                if (keyValuepair.Value == 1)
                {
                    rootAsset.Add(keyValuepair.Key);
                }
            }
            rootAsset.Sort();
            
            //开始构建隐式AssetBundle目录
            List<BundleBuildInfo> bundleBuildInfos = new List<BundleBuildInfo>();
            int index = 0;
            foreach (string assetname in rootAsset)
            {
                BundleBuildInfo buildInfo = new BundleBuildInfo(string.Empty, $"BundleShare{index}.bundle", "Base", EditorBundleEncrypt.Global);
                bundleBuildInfos.Add(buildInfo);
                buildInfo.AssetBuildInfos.Add(new AssetBuildInfo(assetname, buildInfo.BundlePath, Path.GetFileName(assetname)));
                List<string> assetDepends = NatAssetEditorUtil.GetDependencies(assetname);
                foreach (string assetDepend in assetDepends)
                {
                    buildInfo.AssetBuildInfos.Add(new AssetBuildInfo(assetDepend, buildInfo.BundlePath, Path.GetFileName(assetDepend)));
                }
                index++;
            }
            return bundleBuildInfos;
        }

        private static List<BundleBuildInfo> AddSharedBundle(List<BundleBuildInfo> bundleBuildInfos, List<BundleBuildInfo> sharedBundle)
        {
            foreach (BundleBuildInfo bundleBuildInfo in sharedBundle)
            {
                bundleBuildInfos.Add(bundleBuildInfo);
            }
            return bundleBuildInfos;
        }
        /// <summary>
        /// 分割场景包和普通包
        /// </summary>
        /// <param name="bundleBuildInfos"></param>
        /// <returns></returns>
        private static List<BundleBuildInfo> SplitSceneBundle(List<BundleBuildInfo> bundleBuildInfos)
        {
            if (bundleBuildInfos == null) return null;

            List<BundleBuildInfo> result = new List<BundleBuildInfo>();
            for (int i = 0; i < bundleBuildInfos.Count; i++)
            {
                BundleBuildInfo bundleBuildInfo = bundleBuildInfos[i];
                List<AssetBuildInfo> sceneInfo = new List<AssetBuildInfo>();
                List<AssetBuildInfo> commonInfo = new List<AssetBuildInfo>();
                List<AssetBuildInfo> assetBuildInfos = bundleBuildInfo.AssetBuildInfos;
                for (int j = 0; j < assetBuildInfos.Count; j++)
                {
                    AssetBuildInfo assetBuildInfo = assetBuildInfos[j];
                    if (assetBuildInfo.AssetName.EndsWith(".unity"))
                    {
                        sceneInfo.Add(assetBuildInfo);
                    }
                    else
                    {
                        commonInfo.Add(assetBuildInfo);
                    }
                }

                if (sceneInfo.Count == 0 || commonInfo.Count == 0)
                {
                    result.Add(bundleBuildInfo);
                    continue;
                }

                //只保留场景资源
                bundleBuildInfo.AssetBuildInfos = sceneInfo;
                result.Add(bundleBuildInfo);

                //构建场景资源额外包
                string[] spliteName = bundleBuildInfo.CompletePath.Split('.');
                string bundlePath = spliteName[0] + "_extra";
                BundleBuildInfo normalBundleInfo = new BundleBuildInfo(bundleBuildInfo.EditorPath, bundlePath,
                    bundleBuildInfo.BundleGroup, bundleBuildInfo.BundleEncrypt);
                normalBundleInfo.AssetBuildInfos = commonInfo;

                result.Add(normalBundleInfo);
            }
            return result;
        }

        private static void AnaylzeBundleAssetsSize(List<BundleBuildInfo> bundleInfos)
        {
            foreach (BundleBuildInfo bundleBuildInfo in bundleInfos)
            {
                bundleBuildInfo.RefreshAllLength();
            }
        }
    }
}