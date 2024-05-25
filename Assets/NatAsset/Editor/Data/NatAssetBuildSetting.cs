using System;
using System.Collections.Generic;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [CreateAssetMenu(fileName = "NatAssetBuildSetting", menuName = "NATFramework/NatAssetBuildSetting", order = 1)]
    internal class NatAssetBuildSetting : ScriptableObject, IEditorReflect
    {
        private static NatAssetBuildSetting instance = null;

        internal static NatAssetBuildSetting Instance
        {
            get
            {
                if (instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{nameof(NatAssetBuildSetting)}");
                    if (guids.Length == 0)
                    {
                        AssetDatabase.CreateAsset(new NatAssetBuildSetting(), "Assets/NatAssetBuildSetting.asset");
                        guids = AssetDatabase.FindAssets($"t:{nameof(NatAssetBuildSetting)}");
                    }

                    string path;
                    if (guids.Length > 1)
                    {
                        Debug.LogWarning($"创建了多个{nameof(NatAssetBuildSetting)}");
                        foreach (string guid in guids)
                        {
                            path = AssetDatabase.GUIDToAssetPath(guid);
                            Debug.LogWarning(path);
                        }
                    }
                    
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    instance = AssetDatabase.LoadAssetAtPath<NatAssetBuildSetting>(path);
                    instance.InitBundleCollector();
                }

                return instance;
            }
        }

        internal void InitBundleCollector()
        {
            if(CollectorDic == null)
                CollectorDic = new Dictionary<string, IBuildCollector>();
            CollectorDic.Clear();
            List<IBuildCollector> collectors = Extensions.GetInheritTypeObjects<IBuildCollector>();
            for (int i = 0; i < collectors.Count; i++)
            {
                IBuildCollector buildCollector = collectors[i];
                CollectorDic.Add(buildCollector.GetType().Name, buildCollector);
            }
        }

        internal Dictionary<string, IBuildCollector> CollectorDic;

        public string GetOutPutPathByRuntime => Instance.OutPutPath;

        /// <summary>
        /// 编辑器下的目标平台
        /// </summary>
        [SerializeField] internal ValidBuildTarget EditorTargetPlatform = ValidBuildTarget.StandaloneWindows64;

        /// <summary>
        /// 压缩方式
        /// </summary>
        [SerializeField] internal CompressOptions BuildCompression = CompressOptions.ChunkBasedCompression;

        /// <summary>
        /// 全局加密方式
        /// </summary>
        [SerializeField] internal GloableEncrypt GlobalBundleEncrypt = GloableEncrypt.Nono;

        /// <summary>
        /// 编辑器下资源输出目录
        /// </summary>
        [SerializeField] internal string OutPutPath = "AssetBundles";

        internal string ClearDirectory = "";

        /// <summary>
        /// 打包时的目标平台
        /// </summary>
        [SerializeField] internal List<BuildPlatform> BuildPlatforms;

        [SerializeField] internal bool AppendHash;

        [SerializeField] internal string CopyGroup = String.Empty;

        /// <summary>
        /// 以根目录下的目录为节点设置Bundle
        /// </summary>
        [SerializeField] internal List<PackageVirtualRoot> VirtualFiles;

        /// <summary>
        /// 用于编辑器界面的临时缓存
        /// </summary>
        /// <returns></returns>
        [SerializeField] internal List<BundleBuildInfo> tempBundleBuildInfos;
    }
    [Serializable]
    internal class BuildPlatform
    {
        [SerializeField]
        internal ValidBuildTarget ValidBuildTarget;
        [SerializeField]
        internal bool isSelect;
    }
}