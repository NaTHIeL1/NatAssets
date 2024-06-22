using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static partial class RuntimeData
    {
        //todo:BundleConfig文件需要区分为buildin和hotload两类
        //todo:加入配套的Asset列表，包装成一个完整的对象
        //随包资源有专属的资源表
        //private static RunTimeManifest BuildInConfig = null;
        //热更资源专属资源表
        //private static RunTimeManifest SandBoxConfig = null;

        //private static Dictionary<string, BundleManifest> BundleConfig = null;
        //private static Dictionary<string, AssetManifest> AssetConfig = null;
        //private static Dictionary<string, List<string>> GroupConfig = null;

        private static BaseManifestModeLoader _manifestLoader = null;
        //internal static void InitBundleInventory()
        //{
        //    //加载配置信息
        //    ManifestLoad.LoadManifest(NatAssetSetting.ConfigName, ManifestLoadCallback);

        //    //todo:选择Load模式
        //}

        //private static void ManifestLoadCallback(NatAssetManifest buildInManifest, NatAssetManifest readWriteManifest)
        //{
        //    //List<BundleManifest> bundleList = buildInManifest.BundleManifests;
        //    //Dictionary<string, BundleManifest> bundleDic = new Dictionary<string, BundleManifest>(bundleList.Count);
        //    //Dictionary<string, AssetManifest> assetDic = new Dictionary<string, AssetManifest>();
        //    Dictionary<string, List<string>> groupDic = new Dictionary<string, List<string>>();

        //    //BundleConfig = bundleDic;
        //    //AssetConfig = assetDic;
        //    GroupConfig = groupDic;
        //    //BuildReadOnlyManifest(buildInManifest);
        //    //BuildReadWriteManifest(buildInManifest, readWriteManifest);

        //    BuildBuildInManifest(buildInManifest);
        //    BuildSandBoxManifest(readWriteManifest);

        //    LoadMainfest();
        //    ExeCompleteCallbak();
        //}

        //private static void BuildBuildInManifest(NatAssetManifest buildInManifest)
        //{
        //    if (buildInManifest != null)
        //    {
        //        RunTimeManifest runTimeManifest = new RunTimeManifest(buildInManifest, LoadPath.ReadOnly);
        //        BuildInConfig = runTimeManifest;
        //    }
        //}

        //private static void BuildSandBoxManifest(NatAssetManifest sandBoxManifest)
        //{
        //    if (sandBoxManifest != null)
        //    {
        //        SandBoxConfig = new RunTimeManifest(sandBoxManifest, LoadPath.ReadWrite);
        //    }
        //}

        //private static void BuildReadOnlyManifest(NatAssetManifest natAssetManifest)
        //{
        //    if (natAssetManifest != null)
        //    {
        //        List<BundleManifest> bundleList = natAssetManifest.BundleManifests;
        //        for (int i = 0; i < bundleList.Count; i++)
        //        {
        //            BundleManifest bundleManifest = bundleList[i];
        //            bundleManifest.LoadPath = LoadPath.ReadOnly;
        //            BundleConfig.Add(bundleManifest.BundlePath, bundleManifest);
        //            AddBundleToGroup(bundleManifest, GroupConfig, BundleConfig);
        //            List<AssetManifest> assetManifests = bundleManifest.Assets;
        //            for (int j = 0; j < assetManifests.Count; j++)
        //            {
        //                AssetManifest assetManifest = assetManifests[j];
        //                assetManifest.BundleName = bundleManifest.BundlePath;
        //                AssetConfig.Add(assetManifest.AssetName, assetManifest);
        //            }
        //        }
        //    }
        //}

        //private static void BuildReadWriteManifest(NatAssetManifest readOnlyManifest, NatAssetManifest natAssetManifest)
        //{
        //    if (natAssetManifest != null && ConfigVersion.CompareManifest(readOnlyManifest, natAssetManifest))
        //    {
        //        List<BundleManifest> bundleList = natAssetManifest.BundleManifests;
        //        for (int i = 0; i < bundleList.Count; i++)
        //        {
        //            BundleManifest bundleManifest = bundleList[i];
        //            bundleManifest.LoadPath = LoadPath.ReadWrite;
        //            if (BundleConfig.ContainsKey(bundleManifest.BundlePath))
        //            {
        //                //更新目录覆盖包目录
        //                BundleConfig[bundleManifest.BundlePath] = bundleManifest;
        //            }
        //            else
        //            {
        //                BundleConfig.Add(bundleManifest.BundlePath, bundleManifest);
        //            }

        //            AddBundleToGroup(bundleManifest, GroupConfig, BundleConfig);
        //            List<AssetManifest> assetManifests = bundleManifest.Assets;
        //            for (int j = 0; j < assetManifests.Count; j++)
        //            {
        //                AssetManifest assetManifest = assetManifests[j];
        //                if (AssetConfig.TryGetValue(assetManifest.AssetName, out AssetManifest runtimeManifest))
        //                {
        //                    runtimeManifest.AssetName = assetManifest.AssetName;
        //                    runtimeManifest.BundleName = bundleManifest.BundlePath;
        //                }
        //                else
        //                {
        //                    assetManifest.BundleName = bundleManifest.BundlePath;
        //                    AssetConfig.Add(assetManifest.AssetName, assetManifest);
        //                }
        //            }
        //        }
        //    }
        //}

        private static void AddBundleToGroup(BundleManifest bundleManifest, Dictionary<string, List<string>> targetGroup, Dictionary<string, BundleManifest> bundleConfig)
        {
            static void AddBundlePathToGroup(List<string> targetGroup, string bundlePath)
            {
                if (!targetGroup.Contains(bundlePath))
                    targetGroup.Add(bundlePath);
            }

            if (targetGroup.TryGetValue(bundleManifest.MainGroup, out List<string> bundles))
            {
                if (!bundles.Contains(bundleManifest.BundlePath))
                {
                    AddBundlePathToGroup(bundles, bundleManifest.BundlePath);
                    string[] dependenceList = bundleManifest.Dependencies;
                    if (dependenceList != null)
                    {
                        for (int i = 0; i < dependenceList.Length; i++)
                        {
                            BundleManifest temp = null;
                            if (bundleConfig.TryGetValue(dependenceList[i], out temp))
                            {
                                AddBundlePathToGroup(bundles, temp.BundlePath);
                            }
                        }
                    }
                }
            }
            else
            {
                List<string> bundleList = new List<string>();
                targetGroup.Add(bundleManifest.MainGroup, bundleList);
                AddBundlePathToGroup(bundleList, bundleManifest.BundlePath);
                string[] dependenceList = bundleManifest.Dependencies;
                if (dependenceList != null)
                {
                    for (int i = 0; i < dependenceList.Length; i++)
                    {
                        BundleManifest temp = null;
                        if (bundleConfig.TryGetValue(dependenceList[i], out temp))
                        {
                            AddBundlePathToGroup(bundleList, temp.BundlePath);
                        }
                    }
                }

            }
        }



        internal static Dictionary<string, string> GetAllBundleMD5()
        {
            static void BuildFunc(Dictionary<string, BundleManifest> runTimeManifest, Dictionary<string, string> target)
            {
                if (runTimeManifest != null)
                {
                    foreach (KeyValuePair<string, BundleManifest> keyValue in runTimeManifest)
                    {
                        if (!target.ContainsKey(keyValue.Key))
                        {
                            target.Add(keyValue.Key, keyValue.Value.MD5);
                        }
                    }
                }
            }
            
            Dictionary<string, string> _bundleMD5Dic = new Dictionary<string, string>(1000);

            Dictionary<string, BundleManifest> _source = _manifestLoader.GetBundleManifestDic();
            BuildFunc(_source, _bundleMD5Dic);
            return _bundleMD5Dic;
        }

        internal static string GetBundleMD5(string bundlePath)
        {
            BundleManifest bundleManifest = null;
            if(_manifestLoader != null)
                bundleManifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (bundleManifest != null)
            {
                return bundleManifest.MD5;
            }

            return null;
        }

        internal static string[] GetAllDependencies(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                return manifest.Dependencies;
            return new string[0];
        }

        internal static uint GetBundleCRC(string bundlePath)
        {
            BundleManifest manifest = null;
            if(_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                return manifest.CRC;
            return 0u;
        }

        internal static BundleEncrypt GetBundleEncrypt(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                return manifest.BundleEncrypt;
            return BundleEncrypt.Nono;
        }

        internal static LoadPath GetBundleLoadPath(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                return manifest.LoadPath;
            return LoadPath.ReadOnly;
        }

        internal static string GetBundleRelativePath(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            return manifest == null ? bundlePath : manifest.RelativePath;
        }

        internal static Hash128 GetAssetBundleHash(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                Hash128.Parse(manifest.Hash);
            return default(Hash128);
        }

        internal static bool CheckHasBundle(string bundlePath)
        {
            BundleManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetBundleManifest(bundlePath);
            if (manifest != null)
                return true;
            return false;
        }

        internal static AssetManifest GetAssetManifest(string assetPath)
        {
            AssetManifest manifest = null;
            if (_manifestLoader != null)
                manifest = _manifestLoader.GetAssetManifest(assetPath);
            return manifest;
        }
    }
}