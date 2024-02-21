using System;
using System.IO;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
#if NATASSET_SBP_SUPPORT
using UnityEditor.Build.Pipeline.Utilities;
#endif
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class BuildView : ISubView
    {
        #region 压缩选项属性

        GUIContent[] m_CompressionOptions =
        {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
        };

        GUIContent m_CompressionContent =
            new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");

        int[] m_CompressionValues = {0, 1, 2};

        #endregion

        #region 加密选项属性

        private GUIContent[] m_GlobalEncrypts =
        {
            new GUIContent("Nono"),
            new GUIContent("Offset"),
        };

        private GUIContent m_GlobalEncryptContent = new GUIContent("GlobalEncrypt");
        private int[] m_GlobalEncryptValues = {0, 1, 2};

        #endregion

        #region RectLayout

        static class Styles
        {
            public static GUIStyle background = "RL Background";
            public static GUIStyle headerBackground = "RL Header";
        }

        private float topLine = 0.45f;
        private float centerLine = 0.5f;

        private Rect GetLeftBottom
        {
            get
            {
                return new Rect(0, _buildBundleWindow.position.height * topLine,
                    _buildBundleWindow.position.width * centerLine,
                    _buildBundleWindow.position.height * (1 - topLine));
            }
        }

        private Rect GetRightBottom
        {
            get
            {
                return new Rect(GetLeftBottom.width, _buildBundleWindow.position.height * topLine,
                    _buildBundleWindow.position.width * (1 - centerLine),
                    _buildBundleWindow.position.height * (1 - topLine));
            }
        }

        #endregion

        private BuildBundleWindow _buildBundleWindow;

        public void EditorWindow(EditorWindow editorWindow)
        {
            _buildBundleWindow = editorWindow as BuildBundleWindow;
        }

        public void OnEnable()
        {
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(10);

            var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
            centeredStyle.alignment = TextAnchor.UpperCenter;
            GUILayout.Label(new GUIContent("Build setup"), centeredStyle);

            // using (new EditorGUILayout.HorizontalScope())
            // {
            //     EditorGUILayout.LabelField("BuildTarget:", new[] {GUILayout.Width(150)});
            //     NatAssetEditorUtil.TargetPlatform =
            //         (ValidBuildTarget) EditorGUILayout.EnumPopup(NatAssetEditorUtil.TargetPlatform,
            //             new[] {GUILayout.ExpandWidth(true)});
            // }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("ChooseBatchBuild:", new[] {GUILayout.Width(150)});
                foreach (ValidBuildTarget keyValue in Enum.GetValues(typeof(ValidBuildTarget)))
                {
                    bool active = EditorGUILayout.Toggle("    " + keyValue,
                        NatAssetEditorUtil.GetBuildPlatforms(keyValue));
                    NatAssetEditorUtil.SetBuildPlatforms(keyValue, active);
                    NatAssetEditorUtil.SetSettingDirty();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Output Path:", new[] {GUILayout.Width(150)});
                GUILayout.TextField(
                    Path.Combine(NatAssetEditorUtil.OutPutPath)
                        .Replace("\\", "/"),
                    new[] {GUILayout.ExpandWidth(true)});
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Version:", new[] {GUILayout.Width(150)});
                string version = VersionData.NowVersion().ToString();
                GUILayout.TextField(version, new[] {GUILayout.ExpandWidth(true)});
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                NatAssetEditorUtil.BuildCompression = (CompressOptions) EditorGUILayout.IntPopup(
                    m_CompressionContent, (int) NatAssetEditorUtil.BuildCompression,
                    m_CompressionOptions, m_CompressionValues, new[] {GUILayout.Width(405)});
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                NatAssetEditorUtil.GloableEncrypt = (GloableEncrypt) EditorGUILayout.IntPopup(
                    m_GlobalEncryptContent, (int) NatAssetEditorUtil.GloableEncrypt,
                    m_GlobalEncrypts, m_GlobalEncryptValues, new[] {GUILayout.Width(405)});
            }

            if (GUILayout.Button("BundleBuild"))
            {
                VersionData buildVersion = VersionData.NowVersion();
                foreach (ValidBuildTarget keyValue in Enum.GetValues(typeof(ValidBuildTarget)))
                {
                    bool active = NatAssetEditorUtil.GetBuildPlatforms(keyValue);
                    if (active)
                        NatBuildConfig.INTERFACE_Build(keyValue, buildVersion);
                }
            }
            
            DrawLeftButtomView();
            //DrawRightButtomView();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制左下清理模块
        /// </summary>
        private void DrawLeftButtomView()
        {
            GUILayout.BeginArea(GetLeftBottom, Styles.background);
            EditorGUILayout.LabelField("ClearModule", Styles.headerBackground);

            if (GUILayout.Button("清理StreamingAsset目录"))
                ClearStream();
            if (GUILayout.Button("清理构建缓存"))
            {
#if NATASSET_SBP_SUPPORT
                BuildCache.PurgeCache(true);
#endif
                Debug.Log("构建缓存已清除");
            }

            if (GUILayout.Button("清理图集缓存"))
            {
                string atlasCachePath = "Library/AtlasCache";
                if (!Directory.Exists(atlasCachePath))
                {
                    Debug.Log("图集缓存目录不存在");
                    return;
                }

                if (EditorUtility.DisplayDialog("提示", "是否确定清理图集缓存？", "是", "否"))
                {
                    Directory.Delete(atlasCachePath, true);
                    Debug.Log("图集缓存已清理");
                }
            }

            GUILayout.EndArea();
        }

        ///// <summary>
        ///// 绘制右下BundleGroup拷贝模块
        ///// </summary>
        //private void DrawRightButtomView()
        //{
        //    GUILayout.BeginArea(GetRightBottom, Styles.background);
        //    EditorGUILayout.LabelField("BundleGroupModule", Styles.headerBackground);
            
        //    //文件选择
        //    GUILayout.BeginHorizontal();
        //    NatAssetEditorUtil.ClearDirectory = GUILayout.TextField(NatAssetEditorUtil.ClearDirectory);
        //    if (GUILayout.Button("选择目录"))
        //    {
        //        string folder = EditorUtility.OpenFolderPanel("选择要使用的资源目录", NatAssetEditorUtil.ClearDirectory, "");
        //        if (folder != string.Empty)
        //        {
        //            NatAssetEditorUtil.ClearDirectory = folder;
        //        }
        //    }
        //    GUILayout.EndHorizontal();
            
        //    GUILayout.BeginHorizontal();
        //    EditorGUILayout.LabelField("TargetPlatform:", new[] {GUILayout.Width(150)});
        //    NatAssetEditorUtil.EditorTargetPlatform =
        //        (ValidBuildTarget) EditorGUILayout.EnumPopup(NatAssetEditorUtil.EditorTargetPlatform);
        //    GUILayout.EndHorizontal();
            
        //    EditorGUILayout.LabelField("复制的资源组(以英文分号分隔，无输入则全部复制):", GUILayout.Width(300));
        //    NatAssetEditorUtil.CopyGroup =
        //        EditorGUILayout.TextField(NatAssetEditorUtil.CopyGroup, GUILayout.ExpandWidth(true));

        //    if (GUILayout.Button("拷贝资源至StreamingAsset"))
        //    {
        //        //先清理后拷贝
        //        ClearStream();
        //        //todo:拷贝资源，需要准备通用接口
        //    }

        //    if (GUILayout.Button("test API"))
        //    {
        //        Debug.Log(ConfigVersion.CreateVersionData());
        //    }
        //    GUILayout.EndArea();
        //}

        private void ClearFile()
        {
            string outPut = Path.Combine(NatAssetEditorUtil.OutPutPath,
                NatAssetEditorUtil.TargetPlatformsStr);
            string path = Path.Combine(Directory.GetCurrentDirectory(), outPut);
            if (Directory.Exists(path))
            {
                FileAttributes attributes = File.GetAttributes(path);
                if (attributes == FileAttributes.Directory)
                {
                    Directory.Delete(path, true);
                }
            }

            Debug.Log($"{NatAssetEditorUtil.EditorTargetPlatform}清除成功");
        }

        private void ClearStream()
        {
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.Delete(Application.streamingAssetsPath, true);
                AssetDatabase.Refresh();
            }

            Debug.Log($"StreaingAssetsPath:清除成功");
        }

        // private void CopyToStreamAll()
        // {
        //     if (!Directory.Exists(Application.streamingAssetsPath))
        //     {
        //         Directory.CreateDirectory(Application.streamingAssetsPath);
        //     }
        //
        //     string outPut = Path.Combine(NatAssetEditorUtil.OutPutPath,
        //         NatAssetEditorUtil.TargetPlatformsStr).Replace("\\", "/");
        //     string targetPath = Path.Combine(Application.streamingAssetsPath.Replace(Application.dataPath, "Assets"));
        //     CopyDicToNewPath(outPut, targetPath);
        //     AssetDatabase.Refresh();
        // }
        //
        // private void CopyIncludeToStream()
        // {
        //     if (!Directory.Exists(Application.streamingAssetsPath))
        //     {
        //         Directory.CreateDirectory(Application.streamingAssetsPath);
        //     }
        //
        //     NatAssetManifest natAssetConfig = NatAssetEditorUtil.GetNatAssetManifest();
        //     List<BundleManifest> bundleNode = natAssetConfig.BundleManifests;
        //     for (int i = 0; i < bundleNode.Count; i++)
        //     {
        //         BundleManifest bundleManifest = bundleNode[i];
        //         NatAssetEditorUtil.SpliteFilePath(bundleManifest.BundlePath, out string path, out string bundleName);
        //         Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, path));
        //         File.Copy(
        //             Path.Combine(Directory.GetCurrentDirectory(), NatAssetEditorUtil.OutPutPath,
        //                 NatAssetEditorUtil.TargetPlatformsStr, bundleManifest.BundlePath),
        //             Path.Combine(Application.streamingAssetsPath, bundleManifest.BundlePath));
        //         Debug.Log(Directory.GetCurrentDirectory());
        //     }
        //
        //     //拷贝Config文件和平台文件至StreamAsset文件下
        //     string basePath = Path.Combine(Directory.GetCurrentDirectory(), NatAssetEditorUtil.OutPutPath,
        //         NatAssetEditorUtil.TargetPlatformsStr);
        //     File.Copy(Path.Combine(basePath, NatAssetEditorUtil.ConfigName),
        //         Path.Combine(Application.streamingAssetsPath, NatAssetEditorUtil.ConfigName));
        //     File.Copy(Path.Combine(basePath, NatAssetEditorUtil.TargetPlatformsStr),
        //         Path.Combine(Application.streamingAssetsPath,
        //             NatAssetEditorUtil.TargetPlatformsStr));
        //     AssetDatabase.Refresh();
        // }
        //
        // private void CopyDicToNewPath(string sourcePath, string targetPath)
        // {
        //     if (!Directory.Exists(targetPath))
        //     {
        //         Directory.CreateDirectory(targetPath);
        //     }
        //
        //     try
        //     {
        //         string[] labDirs = Directory.GetDirectories(sourcePath); //目录
        //         string[] labFiles = Directory.GetFiles(sourcePath); //文件
        //         if (labFiles.Length > 0)
        //         {
        //             for (int i = 0; i < labFiles.Length; i++)
        //             {
        //                 string extension = Path.GetExtension(labFiles[i]);
        //                 if (extension == ".json" || extension == ".manifest")
        //                     continue;
        //                 File.Copy(sourcePath + "\\" + Path.GetFileName(labFiles[i]),
        //                     targetPath + "\\" + Path.GetFileName(labFiles[i]), true);
        //             }
        //         }
        //
        //         if (labDirs.Length > 0)
        //         {
        //             for (int j = 0; j < labDirs.Length; j++)
        //             {
        //                 Directory.GetDirectories(sourcePath + "\\" + Path.GetFileName(labDirs[j]));
        //                 CopyDicToNewPath(sourcePath + "\\" + Path.GetFileName(labDirs[j]),
        //                     targetPath + "\\" + Path.GetFileName(labDirs[j]));
        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogError(e);
        //     }
        // }
    }
}