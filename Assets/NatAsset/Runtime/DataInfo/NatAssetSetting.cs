﻿namespace NATFrameWork.NatAsset.Runtime
{
    public static class NatAssetSetting
    {
        //todo:后续参数开放为外部可用
        public static int BundleDelayTime { get; set; } = 120;
        public static int AssetDelayTime { get; set; } = 60;
        public static int TaskFrameLimitLoaded { get; set; } = 50;
        public static int TaskFrameLimitUnLoad { get; set; } = 50;
        public static int TaskFrameLimitNetLoad { get; set; } = 50;

        public static bool WebGLIsUseCache { get; set; } = true;
        public static bool UseCRC { get; set; } = true;
        
        internal static RunWay TRunWay;

        /// <summary>
        /// 配置文件名
        /// </summary>
        public static readonly string ConfigName = "NatAssetConfig.data";

        /// <summary>
        /// NatAsset版本
        /// </summary>
        public static readonly int NatAssetVersion = 1;

        /// <summary>
        /// 资源服务器地址
        /// </summary>
        public static string AssetServerURL = string.Empty;

        /// <summary>
        /// 下载资源时的临时文件扩展名
        /// </summary>
        internal const string CacheExtension = ".temporary";

#if UNITY_EDITOR
        public static readonly string SPECIAL_EditorFile = NatAssetPathTool.SPECIAL_EditorFile;
#endif

        public static string ReadOnlyPath => NatAssetPathTool.ReadOnlyPath;
        public static string ReadWritePath => NatAssetPathTool.ReadWritePath;
        public static string DownLoadPath => NatAssetPathTool.CacheDownLoadPath;
        public static string RemotePath => NatAssetPathTool.RemotePath;

        internal static string EditorLoadPath
        {
            get
            {
#if UNITY_EDITOR
                return NatAssetPathTool.EditorLoadPath;
#endif
                return string.Empty;
            }
        }

        internal static void InitData()
        {
#if UNITY_EDITOR
            TRunWay = NatAssetSObj.Instance.RunWay;
            return;
#endif
            TRunWay = RunWay.PackageOnly;
        }

        internal static void ReleaseData()
        {
        }
    }

    public enum RunWay
    {
        Editor = 1,
        PackageOnly = 2,
    }

    internal enum LoadPath
    {
        ReadOnly,
        ReadWrite,
        Remote,
    }
}