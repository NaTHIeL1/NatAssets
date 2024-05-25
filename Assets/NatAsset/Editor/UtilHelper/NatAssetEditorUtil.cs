using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public static class NatAssetEditorUtil
    {
        /// <summary>
        /// 要排除的文件后缀名集合
        /// </summary>
        internal static readonly HashSet<string> ExcludeSet = new HashSet<string>()
        {
            ".meta",
            ".cs",
            ".asmdef",
            ".asmref",
            ".giparams",
            ".so",
            ".dll",
            ".cginc",
        };

        /// <summary>
        /// 名字与构建管线中的一致
        /// </summary>
        internal const string BuiltInShadersName = "UnityBuiltInShaders.bundle";

        public static string ConfigName
        {
            get { return NatAssetSetting.ConfigName; }
        }

        public static string ReadOnlyPath
        {
            get { return NatAssetSetting.ReadOnlyPath; }
        }

        /// <summary>
        /// 编辑器下用于模拟assetbundle运行的文件路径
        /// </summary>
        public static string SPECIAL_EditorFile
        {
            get { return NatAssetSetting.SPECIAL_EditorFile; }
        }

        /// <summary>
        /// 获取AB资源的输出路径
        /// </summary>
        public static string GetEditorPath
        {
            get { return Path.Combine(Directory.GetCurrentDirectory(), NatAssetBuildSetting.Instance.OutPutPath); }
        }

        /// <summary>
        /// 当前编辑器下的目标平台
        /// </summary>
        /// <returns></returns>
        internal static ValidBuildTarget GetEditorBuildTargetActive()
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            int buildTargetInt = Convert.ToInt32(buildTarget);
            foreach (int value in Enum.GetValues(typeof(ValidBuildTarget)))
            {
                if (buildTargetInt == value)
                {
                    ValidBuildTarget validBuildTarget = (ValidBuildTarget)buildTarget;
                    return validBuildTarget;
                }
            }

            Debug.LogError("NatAsset不支持当前平台");
            return default;
        }

        /// <summary>
        /// 缓存的目标平台选项
        /// </summary>
        internal static ValidBuildTarget EditorTargetPlatform
        {
            get { return NatAssetBuildSetting.Instance.EditorTargetPlatform; }
            set { NatAssetBuildSetting.Instance.EditorTargetPlatform = value; }
        }

        internal static List<BundleBuildInfo> TempBundleBuildInfos
        {
            get
            {
                List<BundleBuildInfo> temp = NatAssetBuildSetting.Instance.tempBundleBuildInfos;
                if (temp == null)
                {
                    temp = new List<BundleBuildInfo>();
                    NatAssetBuildSetting.Instance.tempBundleBuildInfos = temp;
                }

                return temp;
            }
            set { NatAssetBuildSetting.Instance.tempBundleBuildInfos = value; }
        }

        internal static void SetBuildPlatforms(ValidBuildTarget buildTarget, bool active)
        {
            List<BuildPlatform> buildPlatforms = NatAssetBuildSetting.Instance.BuildPlatforms;
            if (buildPlatforms == null)
                buildPlatforms = new List<BuildPlatform>();
            for (int i = 0; i < buildPlatforms.Count; i++)
            {
                if (buildPlatforms[i].ValidBuildTarget == buildTarget)
                {
                    buildPlatforms[i].isSelect = active;
                    return;
                }
            }

            buildPlatforms.Add(new BuildPlatform()
            {
                ValidBuildTarget = buildTarget,
                isSelect = active
            });
        }

        internal static bool GetBuildPlatforms(ValidBuildTarget buildTarget)
        {
            List<BuildPlatform> buildPlatforms = NatAssetBuildSetting.Instance.BuildPlatforms;
            if (buildPlatforms == null) return false;
            for (int i = 0; i < buildPlatforms.Count; i++)
            {
                if (buildPlatforms[i].ValidBuildTarget == buildTarget)
                {
                    return buildPlatforms[i].isSelect;
                }
            }

            return false;
        }

        internal static bool AppendHash
        {
            get { return NatAssetBuildSetting.Instance.AppendHash; }
            set { NatAssetBuildSetting.Instance.AppendHash = value; }
        }

        internal static CompressOptions BuildCompression
        {
            get { return NatAssetBuildSetting.Instance.BuildCompression; }
            set { NatAssetBuildSetting.Instance.BuildCompression = value; }
        }

        internal static GloableEncrypt GloableEncrypt
        {
            get { return NatAssetBuildSetting.Instance.GlobalBundleEncrypt; }
            set { NatAssetBuildSetting.Instance.GlobalBundleEncrypt = value; }
        }

        internal static string OutPutPath
        {
            get { return NatAssetBuildSetting.Instance.OutPutPath; }
            set { NatAssetBuildSetting.Instance.OutPutPath = value; }
        }

        internal static string ClearDirectory
        {
            get { return NatAssetBuildSetting.Instance.ClearDirectory; }
            set { NatAssetBuildSetting.Instance.ClearDirectory = value; }
        }

        internal static string CopyGroup
        {
            get { return NatAssetBuildSetting.Instance.CopyGroup; }
            set { NatAssetBuildSetting.Instance.CopyGroup = value; }
        }

        internal static string TargetPlatformsStr
        {
            get { return NatAssetBuildSetting.Instance.EditorTargetPlatform.ToString(); }
        }

        internal static void SetSettingDirty()
        {
            EditorUtility.SetDirty(NatAssetBuildSetting.Instance);
        }

        internal static Dictionary<string, IBuildCollector> BundleCollectorDic
        {
            get { return NatAssetBuildSetting.Instance.CollectorDic; }
        }

        internal static string[] BundleCollectorList
        {
            get
            {
                List<string> strList = new List<string>();
                foreach (var keyValue in BundleCollectorDic)
                {
                    strList.Add(keyValue.Key);
                }

                return strList.ToArray();
            }
        }

        internal static object InvokeNonPublicStaticMethod(Type type, string method, params object[] parameters)
        {
            var methodInfo = type.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                Debug.LogError($"类型：{type.FullName} 中未找到方法: {method}");
                return null;
            }

            return methodInfo.Invoke(null, parameters);
        }

        internal static List<string> GetBundleGroups()
        {
            //todo:扩展接口
            if (CopyGroup != string.Empty)
            {
                List<string> strList = new List<string>();
            }
            return null;
        }

        internal static void CopyAssetToSpecialEditorPath(ValidBuildTarget buildTarget, string sourcePath)
        {
            //组装特殊Editor目录
            string editorPath = Path.Combine(NatAssetEditorUtil.OutPutPath,
                 buildTarget.ToString(), SPECIAL_EditorFile).Replace("\\", "/");
            CopyDicToNewPath(sourcePath, editorPath);
        }

        internal static void CopyDicToNewPath(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                Directory.Delete(targetPath, true);
                Directory.CreateDirectory(targetPath);
            }

            try
            {
                string[] labDirs = Directory.GetDirectories(sourcePath); //目录
                string[] labFiles = Directory.GetFiles(sourcePath); //文件
                if (labFiles.Length > 0)
                {
                    for (int i = 0; i < labFiles.Length; i++)
                    {
                        string extension = Path.GetExtension(labFiles[i]);
                        if (extension == ".manifest")
                            continue;
                        File.Copy(sourcePath + "\\" + Path.GetFileName(labFiles[i]),
                            targetPath + "\\" + Path.GetFileName(labFiles[i]), true);
                    }
                }

                if (labDirs.Length > 0)
                {
                    for (int j = 0; j < labDirs.Length; j++)
                    {
                        Directory.GetDirectories(sourcePath + "\\" + Path.GetFileName(labDirs[j]));
                        CopyDicToNewPath(sourcePath + "\\" + Path.GetFileName(labDirs[j]),
                            targetPath + "\\" + Path.GetFileName(labDirs[j]));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        internal static void CopyFileToNewPath(string sourcePath, string targetPath)
        {
            string directName = Path.GetDirectoryName(targetPath);
            if(!Directory.Exists(directName))
                Directory.CreateDirectory(directName);
            if(File.Exists(targetPath))
                File.Delete(targetPath);
            File.Copy(sourcePath, targetPath, true );
        }

        /// <summary>
        /// 是否为有效资源
        /// </summary>
        public static bool IsValidAsset(string assetName)
        {
            if (AssetDatabase.IsValidFolder(assetName))
                return false;

            string fileExtension = Path.GetExtension(assetName);
            if (string.IsNullOrEmpty(fileExtension))
                return false;
            if (ExcludeSet.Contains(fileExtension))
                return false;
            Type type = AssetDatabase.GetMainAssetTypeAtPath(assetName);
            if (type == typeof(LightingDataAsset))
                return false;

            return true;

        }

        /// <summary>
        /// 打开指定目录
        /// </summary>
        public static void OpenDirectory(string directory)
        {
            directory = string.Format("\"{0}\"", directory);

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                System.Diagnostics.Process.Start("Explorer.exe", directory.Replace('/', '\\'));
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                System.Diagnostics.Process.Start("open", directory);
            }
        }

        /// <summary>
        /// 格式化规范Bundle命名
        /// </summary>
        /// <param name="compeletPath"></param>
        /// <returns></returns>
        public static string FormatBundlePath(string compeletPath)
        {
            string tempStr = compeletPath;
#if !NATASSET_SBP_SUPPORT
            tempStr = compeletPath.ToLower();
#endif
            return tempStr.Split('.')[0] + ".bundle";
        }

        /// <summary>
        /// 转换编辑器下的加密选项为运行时加密选项
        /// </summary>
        /// <param name="editorBundleEncrypt"></param>
        /// <returns></returns>
        public static BundleEncrypt SwitchBundleEncrypt(EditorBundleEncrypt editorBundleEncrypt)
        {
            switch (editorBundleEncrypt)
            {
                case EditorBundleEncrypt.Global:
                    if (GloableEncrypt == GloableEncrypt.Nono)
                        return BundleEncrypt.Nono;
                    else if (GloableEncrypt == GloableEncrypt.Offset)
                        return BundleEncrypt.Offset;
                    break;
                case EditorBundleEncrypt.Nono:
                    return BundleEncrypt.Nono;
                    break;
                case EditorBundleEncrypt.Offset:
                    return BundleEncrypt.Offset;
                    break;
            }

            return BundleEncrypt.Nono;
        }

        /// <summary>
        /// 获取当前资源所依赖的其他资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="recursive">默认true获取所有依赖，false获取直接依赖</param>
        /// <returns></returns>
        public static List<string> GetDependencies(string assetName, bool recursive = true)
        {
            List<string> result = new List<string>();
            string[] dependencies = AssetDatabase.GetDependencies(assetName, recursive);

            if (dependencies.Length == 0)
                return result;

            for (int i = 0; i < dependencies.Length; i++)
            {
                string dependencyName = dependencies[i];
                if (!IsValidAsset(dependencyName))
                    continue;
                if (dependencyName == assetName)
                    continue;
                result.Add(dependencyName);
            }

            return result;
        }

        public static NatAssetManifest ReadNatAssetManifestFromFile(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                byte[] binaryArray = File.ReadAllBytes(fullPath);
                NatAssetManifest manifest = new NatAssetManifest();
                manifest = NatAssetManifest.DeserializeFromBinary(binaryArray);
                return manifest;
            }
            else
            {
                Debug.LogError($"当前路径：{fullPath} 不存在对于文件");
                return null;
            }
        }

        public static void WriteNatAssetMainfestToFile(string fullPath, NatAssetManifest natAssetManifest)
        {
            if (File.Exists(fullPath))
            {
                //已有文件则立即删除
                File.Delete(fullPath);
            }
            string targetDirect = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(targetDirect))
                Directory.CreateDirectory(targetDirect);

            byte[] binaryArray = natAssetManifest.SerializeToBinary();
            using (FileStream fs = File.Create(fullPath))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(binaryArray);
                }
            }
        }
    }
}