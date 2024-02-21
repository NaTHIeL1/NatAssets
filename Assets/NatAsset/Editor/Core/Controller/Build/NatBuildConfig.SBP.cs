#if NATASSET_SBP_SUPPORT
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using BuildCompression = UnityEngine.BuildCompression;

namespace NATFrameWork.NatAsset.Editor
{
    public static partial class NatBuildConfig
    {
        private static CompatibilityAssetBundleManifest SBPBuild(string outPut, AssetBundleBuild[] assetBundleBuilds,
            BuildAssetBundleOptions bundleOptions, BuildTarget buildTarget,
            NatBundleBuildConfigParam natBundleBuildConfigParam)
        {
            var temp = BuildAssetBundles(outPut, new BundleBuildContent(assetBundleBuilds), bundleOptions,
                buildTarget, natBundleBuildConfigParam);
            return temp;
        }

        private static CompatibilityAssetBundleManifest BuildAssetBundles(string outputPath,
            IBundleBuildContent content, BuildAssetBundleOptions options, BuildTarget targetPlatform,
            NatBundleBuildConfigParam natBundleBuildConfigParam)
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            BundleBuildParameters parameters = GetBundleBuildParameters(targetPlatform, group, outputPath, options);
            IList<IBuildTask> taskList = GetTaskLiat();

            ReturnCode exitCode =
                ContentPipeline.BuildAssetBundles(parameters, content, out IBundleBuildResults results, taskList,
                    natBundleBuildConfigParam);
            if (exitCode < ReturnCode.Success)
                return null;

            var manifest = ScriptableObject.CreateInstance<CompatibilityAssetBundleManifest>();
            manifest.SetResults(results.BundleInfos);
            return manifest;
        }

        /// <summary>
        /// 创建构建任务
        /// </summary>
        /// <returns></returns>
        private static IList<IBuildTask> GetTaskLiat()
        {
            var taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction);
            taskList.Add(new EncryptTask());
            return taskList;
        }

        /// <summary>
        /// 构建Parameters
        /// </summary>
        /// <param name="targetPlatform"></param>
        /// <param name="buildTargetGroup"></param>
        /// <param name="outPutPath"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static BundleBuildParameters GetBundleBuildParameters(BuildTarget targetPlatform,
            BuildTargetGroup buildTargetGroup, string outPutPath, BuildAssetBundleOptions options)
        {
            BundleBuildParameters buildParameters =
                new BundleBuildParameters(targetPlatform, buildTargetGroup, outPutPath);
            if ((options & BuildAssetBundleOptions.ForceRebuildAssetBundle) != 0)
                buildParameters.UseCache = false;

            if ((options & BuildAssetBundleOptions.AppendHashToAssetBundleName) != 0)
                buildParameters.AppendHash = true;
            //获取bundle压缩格式
            buildParameters.BundleCompression = GetBuildCompression(options);

            if ((options & BuildAssetBundleOptions.DisableWriteTypeTree) != 0)
                buildParameters.ContentBuildFlags |= ContentBuildFlags.DisableWriteTypeTree;
            return buildParameters;
        }

        /// <summary>
        /// 获取选定的Bundle压缩格式
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static BuildCompression GetBuildCompression(BuildAssetBundleOptions options)
        {
#if UNITY_2018_3_OR_NEWER
            if ((options & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                return BuildCompression.LZ4;
            else if ((options & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                return BuildCompression.Uncompressed;
            else
                return BuildCompression.LZMA;
#else
            if ((options & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                return BuildCompression.DefaultLZ4;
            else if ((options & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                return BuildCompression.DefaultUncompressed;
            else
                return BuildCompression.DefaultLZMA;
#endif
        }
    }
}
#endif