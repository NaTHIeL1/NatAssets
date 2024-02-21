using System.Collections.Generic;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;

namespace NATFrameWork.NatAsset.Editor
{
    /// <summary>
    /// 资源构建参数
    /// </summary>
    public class NatBuildParamters
    {
        /// <summary>
        /// 输入平级于Assets下的目录
        /// </summary>
        public string OutPutPath
        {
            get => NatAssetEditorUtil.OutPutPath;
        }

        /// <summary>
        /// 打包平台
        /// </summary>
        public ValidBuildTarget BuildTarget;

        /// <summary>
        /// 全局资源压缩格式
        /// </summary>
        public CompressOptions CompressOptions
        {
            get => NatAssetEditorUtil.BuildCompression;
        }

        public GloableEncrypt GloableEncrypt
        {
            get=> NatAssetEditorUtil.GloableEncrypt;
        } 

        /// <summary>
        /// 资源构建日期
        /// </summary>
        public VersionData VersionData = default;

        /// <summary>
        /// 更新编辑器下本地bundle
        /// </summary>
        public bool UpdateSpecailEdito = true;

        /// <summary>
        /// AssetBundle
        /// </summary>
        public List<BundleBuildInfo> BundleBuildInfos;

        public NatBuildParamters(ValidBuildTarget buildTarget, List<BundleBuildInfo> bundleBuildInfos)
        {
            BuildTarget = buildTarget;
            BundleBuildInfos = bundleBuildInfos;
        }
    }
}