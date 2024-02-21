using System;
using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    public abstract class BaseManifestModeLoader
    {
        internal abstract void StartLoader();
        internal abstract void StopLoader();
        internal abstract BundleManifest GetBundleManifest(string bundlePath);
        internal abstract AssetManifest GetAssetManifest(string assetPath);
        internal abstract Dictionary<string, BundleManifest> GetBundleManifestDic();
        internal abstract List<string> GetBundleGroup(string groupName);

        internal static string BuildInLoadPath
        {
            get
            {
#if UNITY_EDITOR
                return NatAssetSetting.EditorLoadPath;
#else
                return NatAssetSetting.ReadOnlyPath;
#endif
            }
        }

        internal static string ReadWriteLoadPath
        {
            get
            {
                return NatAssetSetting.ReadWritePath;
            }
        }

        internal static string RemotePath
        {
            get
            {
                return NatAssetSetting.RemotePath;
            }
        }

        public Action<ManifestModeState> LoadCallBack { get; set; }
    }

    //todo:增加当前状态判断
    public enum ManifestModeState
    {
        Nono,                 //等待配置文件加载
        BuildInState,         //加载随包配置文件（不论什么资源都必有该状态）
        ReadWriteState,       //加载可读写配置文件
        CompareAssetState,    //比对版本校验资源
        UpdateAssetState,     //更新资源阶段
        DecompressAssetState, //资源解压阶段
        Finish,               //成功加载完成
        Error,                //配置文件加载出错
    }
}
