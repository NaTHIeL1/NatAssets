﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;
#if NATASSET_SBP_SUPPORT
using UnityEngine.Build.Pipeline;
#endif

namespace NATFrameWork.NatAsset.Editor
{
    public static partial class NatBuildConfig
    {
#if NATASSET_SBP_SUPPORT
        private static CompatibilityAssetBundleManifest Mainfest = null;
#else
        private static AssetBundleManifest Mainfest = null;
#endif

        internal static void INTERFACE_Build(ValidBuildTarget validBuildTarget, VersionData buildVersion)
        {
            NatBuildParamters natBuildParamters =
                new NatBuildParamters(validBuildTarget, NatAssetBuildUtil.CollectBundles());
            natBuildParamters.BuildTarget = validBuildTarget;
            natBuildParamters.VersionData = buildVersion;
            Build(natBuildParamters);
        }

        internal static void Build(NatBuildParamters natBuildParamters)
        {
#if !NATASSET_SBP_SUPPORT
            UnityEngine.Debug.LogException(new Exception("请导入Scriptable Build Pipeline包再打包资源"));
            return;
#endif
            Stopwatch stopwatch = Stopwatch.StartNew();
            if (natBuildParamters.VersionData == default)
                natBuildParamters.VersionData = VersionData.NowVersion();
            string outPut = Path.Combine(natBuildParamters.OutPutPath, natBuildParamters.BuildTarget.ToString(),
                natBuildParamters.VersionData.ToString()).Replace("\\", "/");
            if (!Directory.Exists(outPut))
            {
                Directory.CreateDirectory(outPut);
            }

            Dictionary<string, BundleBuildInfo> bundleBuildInfos = GetBundleInsBuildDic(natBuildParamters);
            AssetBundleBuild[] assetBundleBuilds =
                NatAssetBuildUtil.SwitchBBIsToABBs(natBuildParamters.BundleBuildInfos);
            BuildTarget buildTarget = (BuildTarget) (int) natBuildParamters.BuildTarget;
            BuildAssetBundleOptions bundleOptions;
            switch (natBuildParamters.CompressOptions)
            {
                case CompressOptions.Uncompressed:
                    bundleOptions = BuildAssetBundleOptions.UncompressedAssetBundle;
                    break;
                case CompressOptions.ChunkBasedCompression:
                    bundleOptions = BuildAssetBundleOptions.ChunkBasedCompression;
                    break;
                default:
                    bundleOptions = BuildAssetBundleOptions.None;
                    break;
            }

            try
            {
#if NATASSET_SBP_SUPPORT
                NatBundleBuildConfigParam natBundleBuildConfigParam =
                    new NatBundleBuildConfigParam(bundleBuildInfos, buildTarget, natBuildParamters.VersionData);
                Mainfest = SBPBuild(outPut, assetBundleBuilds, bundleOptions, buildTarget, natBundleBuildConfigParam);
#else
                Mainfest = BuildPipeline.BuildAssetBundles(outPut, assetBundleBuilds, bundleOptions, buildTarget);
#endif
            }
            catch (Exception e)
            {
                throw e;
            }

            //生成MD5值
            BuildBundleDicMD5(outPut, bundleBuildInfos);
            //生成配置文件
            BuildConfigJson(outPut, natBuildParamters.VersionData, natBuildParamters.BuildTarget,
                bundleBuildInfos);
            stopwatch.Stop();
            UnityEngine.Debug.LogFormat("NatAsset-{0}资源打包成功，耗时：{1}h{2}m{3}s", natBuildParamters.BuildTarget,
                stopwatch.Elapsed.Hours,
                stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds);

            //拷贝资源至SpecialEditor目录
            if (natBuildParamters.UpdateSpecailEdito)
                NatAssetEditorUtil.CopyAssetToSpecialEditorPath(natBuildParamters.BuildTarget, outPut);
        }

        /// <summary>
        /// 构建配置文件
        /// </summary>
        /// <param name="outPutPath"></param>
        /// <param name="IsVersion"></param>
        /// <param name="validBuildTarget"></param>
        /// <param name="bundleInventoryBuilds"></param>
        private static void BuildConfigJson(string outPutPath, VersionData buildVersion,
            ValidBuildTarget validBuildTarget,
            Dictionary<string, BundleBuildInfo> bundleInventoryBuilds)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), outPutPath);

            #region 生成配置文件

            NatAssetManifest natAssetMainfest = new NatAssetManifest();
            natAssetMainfest.BuildVersion = buildVersion.ToString();
            natAssetMainfest.ReleaseVersion = buildVersion.ToString();
            natAssetMainfest.Platform = validBuildTarget.ToString();

            //获取要排除的所有目标文件夹
            List<string> excludeFilePaths = ReflectBuild.ReflectExcludeFileExtension();

            natAssetMainfest.BundleManifests = new List<BundleManifest>();
            List<BundleManifest> bundleList = natAssetMainfest.BundleManifests;

            Dictionary<string, BundleManifest> tempBundleManifestDic = new Dictionary<string, BundleManifest>();
            foreach (var keyValue in bundleInventoryBuilds)
            {
                BundleBuildInfo data = keyValue.Value;
                BundleManifest temp = BuildBundleItemNode(data, excludeFilePaths);
                if (!tempBundleManifestDic.ContainsKey(data.BundlePath))
                    tempBundleManifestDic.Add(data.BundlePath, temp);
                string[] dependBundles = Mainfest.GetAllDependencies(data.BundlePath);
                for (int i = 0; i < dependBundles.Length; i++)
                {
                    if (!tempBundleManifestDic.ContainsKey(dependBundles[i]))
                    {
                        if (!bundleInventoryBuilds.ContainsKey(dependBundles[i]))
                        {
                            UnityEngine.Debug.Log("不存在目标target");
                            continue;
                        }    
                        BundleBuildInfo dependOutput = bundleInventoryBuilds[dependBundles[i]];
                        BundleManifest dependData = BuildBundleItemNode(dependOutput, excludeFilePaths);
                        tempBundleManifestDic.Add(dependOutput.BundlePath, dependData);
                    }
                }
            }

            foreach (KeyValuePair<string, BundleManifest> bundleManifest in tempBundleManifestDic)
            {
                bundleList.Add(bundleManifest.Value);
            }

            #endregion

            #region 数据写入磁盘

            string jsonConfig = JsonUtility.ToJson(natAssetMainfest);
            string filePath = Path.Combine(path, NatAssetEditorUtil.ConfigName);
            byte[] dataArray = natAssetMainfest.SerializeToBinary();

            using (FileStream fs = File.Create(filePath))
            {
                BinaryWriter bw = new BinaryWriter(fs);
                bw.Write(dataArray);
                bw.Close();
            }

            //用于查看生成的Config文件
            using (FileStream fs = File.Create(filePath + ".json"))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(jsonConfig);
                sw.Close();
            }

            #endregion
        }

        /// <summary>
        /// 构建配置文件的BundleManifest部分
        /// </summary>
        /// <param name="data"></param>
        /// <param name="excludeList"></param>
        /// <returns></returns>
        //构建BundleNode中的子节点
        private static BundleManifest BuildBundleItemNode(BundleBuildInfo data, List<string> excludeList)
        {
            BundleManifest temp = new BundleManifest();
            temp.MD5 = data.MD5;
            temp.BundlePath = data.BundlePath;
#if !NATASSET_SBP_SUPPORT
            BuildPipeline.GetCRCForAssetBundle(Path.Combine(Directory.GetCurrentDirectory(),
                NatAssetBuildSetting.Instance.OutPutPath, 
                NatAssetBuildSetting.Instance.BuildPlatforms.ToString(), data.BundlePath), out uint crc);
            temp.CRC = crc;
#else
            temp.CRC = Mainfest.GetAssetBundleCrc(data.BundlePath);
#endif
            temp.Hash = Mainfest.GetAssetBundleHash(data.BundlePath).ToString();
            //todo:bundleGroup修改
            temp.Group = data.BundleGroup;
            temp.IsRaw = false;
            temp.Dependencies = Mainfest.GetDirectDependencies(data.BundlePath);
            temp.BundleEncrypt = NatAssetEditorUtil.SwitchBundleEncrypt(data.BundleEncrypt);
            List<AssetManifest> assets = new List<AssetManifest>();
            BuildAssetMainfest(data, assets, excludeList);
            temp.Assets = assets;
            return temp;
        }

        /// <summary>
        /// 构建配置文件的AssetManifest
        /// </summary>
        /// <param name="data"></param>
        /// <param name="assetManifests"></param>
        /// <param name="excludeList"></param>
        private static void BuildAssetMainfest(BundleBuildInfo data, List<AssetManifest> assetManifests,
            List<string> excludeList)
        {
            //过滤目标文件夹
            if (excludeList.Contains(data.CompletePath.ToLower()))
            {
                return;
            }

            for (int i = 0; i < data.AssetBuildInfos.Count; i++)
            {
                AssetBuildInfo assetBuildInfo = data.AssetBuildInfos[i];
                AssetManifest temp = new AssetManifest();
                temp.AssetName = assetBuildInfo.EditorPath.Replace(@"\", "/");
                //temp.TitleName = Path.Combine(data.CompletePath, assetBuildInfo.AssetName).Replace("\\", "/");
                assetManifests.Add(temp);
            }
        }

        /// <summary>
        /// 通过BundleInventory构建出打包出文件的配置信息，前置构建，无MD5信息
        /// </summary>
        /// <param name="outPath"></param>
        /// <returns></returns>
        private static Dictionary<string, BundleBuildInfo> GetBundleInsBuildDic(NatBuildParamters buildParamters)
        {
            List<BundleBuildInfo> buildInfos = buildParamters.BundleBuildInfos;
            Dictionary<string, BundleBuildInfo> outPutDic = new Dictionary<string, BundleBuildInfo>();
            for (int i = 0; i < buildInfos.Count; i++)
            {
                BundleBuildInfo item = buildInfos[i];
                outPutDic.Add(item.BundlePath, item);
            }
#if NATASSET_SBP_SUPPORT
            BundleBuildInfo build = new BundleBuildInfo("Assets", "", "Base", EditorBundleEncrypt.Global);
            build.CompletePath = string.Empty;
            build.BundlePath = NatAssetEditorUtil.BuiltInShadersName;
            build.BundleGroup = "Base";
            build.BundleEncrypt = EditorBundleEncrypt.Global;

            outPutDic.Add(NatAssetEditorUtil.BuiltInShadersName, build);
#endif
            return outPutDic;
        }

        /// <summary>
        /// 为构建出打包出文件的配置信息生成MD5信息
        /// </summary>
        /// <param name="outPath"></param>
        /// <param name="outPutDic"></param>
        private static void BuildBundleDicMD5(string outPath, Dictionary<string, BundleBuildInfo> outPutDic)
        {
            foreach (var keyValuePair in outPutDic)
            {
                BundleBuildInfo build = keyValuePair.Value;
                string filePath = Path.Combine(outPath, build.BundlePath);
                build.MD5 = GetFileMD5(filePath);
            }
        }

        private static string GetFileMD5(string filePath)
        {
            return FileExtend.GetFileMD5(filePath);
        }
    }
}