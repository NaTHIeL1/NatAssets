using NATFrameWork.NatAsset.Runtime;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class VersionView : ISubView
    {
        private BuildBundleWindow _buildBundleWindow;
        private SearchField _searchField;
        private TreeViewState _versionTreeViewState;
        private VersionTreeView _versionTreeView;

        private float _searchSplit = 0.6f;
        private Rect _rightRect, _leftRect;

        private VersionInfo _versionInfo;

        static class Styles
        {
            public static GUIStyle background = "RL Background";
            public static GUIStyle headerBackground = "RL Header";
        }

        public void EditorWindow(EditorWindow editorWindow)
        {
            _buildBundleWindow = editorWindow as BuildBundleWindow;
        }

        public void OnEnable()
        {
            _versionTreeViewState = _versionTreeViewState == null ? new TreeViewState() : _versionTreeViewState;
            _versionTreeView = new VersionTreeView(_versionTreeViewState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _versionTreeView.SetFocusAndEnsureSelectedItem;
            _versionTreeView.ListenSelectChanged(ListenSelectChanged);
        }

        private void ListenSelectChanged(VersionInfo versionInfo)
        {
            _versionInfo = versionInfo;
        }

        public void OnGUI()
        {
            DrawSearchView();
            DrawVersionView();
            DrawRightView();
        }

        private void DrawSearchView()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            _versionTreeView.searchString = _searchField.OnToolbarGUI(_versionTreeView.searchString,
                new[] { GUILayout.Width(_buildBundleWindow.position.width * _searchSplit) });
            if (GUILayout.Button("Refresh"))
            {
                Refresh();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRightView()
        {
            _rightRect = new Rect(_buildBundleWindow.position.width - 300, 50, 300, _buildBundleWindow.position.height - 50);
            GUILayout.BeginArea(_rightRect, Styles.background);


            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PlatformTarget:", new[] { GUILayout.Width(100) });
            NatAssetEditorUtil.EditorTargetPlatform =
                (ValidBuildTarget)EditorGUILayout.EnumPopup(NatAssetEditorUtil.EditorTargetPlatform);
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("复制的资源组(以英文分号分隔，无输入则全部复制):", GUILayout.Width(300));
            NatAssetEditorUtil.CopyGroup =
                EditorGUILayout.TextArea(NatAssetEditorUtil.CopyGroup, GUILayout.ExpandWidth(true));

            if (_versionInfo != null)
            {
                GUILayout.Label($"当前选中版本:{_versionInfo.Name}");
                if (GUILayout.Button("拷贝资源组至StreamingAsset"))
                {
                    //先清理后拷贝
                    ClearStream();
                    CopyTargetGroupsToStreamingAsset(_versionInfo);
                }

                //todo:资源上传服务器
            }

            if (EditorGUI.EndChangeCheck())
            {
                NatAssetEditorUtil.SetSettingDirty();
                Refresh();
            }
            GUILayout.EndArea();
        }

        private void DrawVersionView()
        {
            _leftRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _leftRect.width = _buildBundleWindow.position.width - 300;
            _versionTreeView.OnGUI(_leftRect);
        }

        private void Refresh()
        {
            _versionTreeView.Reload();
            _versionTreeView.Repaint();
        }

        private void ClearStream()
        {
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.Delete(Application.streamingAssetsPath, true);
                AssetDatabase.Refresh();
            }
        }

        private void CopyTargetGroupsToStreamingAsset(VersionInfo versionInfo)
        {
            if (NatAssetEditorUtil.CopyGroup == null || NatAssetEditorUtil.CopyGroup.Equals(string.Empty))
            {
                //拷贝全部
                NatAssetEditorUtil.CopyDicToNewPath(versionInfo.FullName, NatAssetEditorUtil.ReadOnlyPath);
                return;
            }
            //准备要使用的数据
            NatAssetManifest versionManifest = NatAssetEditorUtil.ReadNatAssetManifestFromFile(Path.Combine(versionInfo.FullName, NatAssetEditorUtil.ConfigName));
            Dictionary<string, BundleManifest> versionBundleDic = new Dictionary<string, BundleManifest>(versionManifest.BundleManifests.Count);
            foreach (BundleManifest manifest in versionManifest.BundleManifests)
            {
                if (!versionBundleDic.ContainsKey(manifest.BundlePath))
                {
                    versionBundleDic.Add(manifest.BundlePath, manifest);
                }
            }

            BundleManifest LocalGetBundleManifestMethod(string bundlePath)
            {
                if (versionBundleDic.ContainsKey(bundlePath))
                {
                    return versionBundleDic[bundlePath];
                }
                return null;
            }

            string[] groups = NatAssetEditorUtil.CopyGroup.Split(';');
            //构建目标配置文件
            NatAssetManifest resultManifest = new NatAssetManifest();
            List<BundleManifest> targetBundleManifests = new List<BundleManifest>();
            List<string> listGroups = new List<string>();
            for(int i = 0; i <groups.Length; i++)
            {
                listGroups.Add(groups[i]);
            }
            resultManifest.NatAssetVersion = versionManifest.NatAssetVersion;
            resultManifest.BuildVersion = versionManifest.BuildVersion;
            resultManifest.ReleaseVersion = versionManifest.ReleaseVersion;
            resultManifest.Platform = versionManifest.Platform;
            resultManifest.IncludeGroups = listGroups;
            resultManifest.BundleManifests = targetBundleManifests;
            
            Dictionary<string, BundleManifest> bundleDic = new Dictionary<string, BundleManifest>();
            List<BundleManifest> versionBundleManifest = versionManifest.BundleManifests;
            for (int i = 0; i < groups.Length; i++)
            {
                string group = groups[i];
                foreach (BundleManifest bundleManifest in versionBundleManifest)
                {
                    if (bundleManifest.MainGroup == group)
                    {
                        if (!bundleDic.ContainsKey(bundleManifest.BundlePath))
                        {
                            bundleDic.Add(bundleManifest.BundlePath, bundleManifest);
                            foreach (string bundlePath in bundleManifest.Dependencies)
                            {
                                if (!bundleDic.ContainsKey(bundlePath))
                                {
                                    BundleManifest bundleManifest2 = LocalGetBundleManifestMethod(bundlePath);
                                    if (bundleManifest2 != null)
                                        bundleDic.Add(bundlePath, bundleManifest2);
                                }
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, BundleManifest> keyValue in bundleDic)
            {
                targetBundleManifests.Add(keyValue.Value);
                string sourcePath = Path.Combine(versionInfo.FullName, keyValue.Value.RelativePath);
                string targetPath = Path.Combine(NatAssetEditorUtil.ReadOnlyPath, keyValue.Value.RelativePath);
                NatAssetEditorUtil.CopyFileToNewPath(sourcePath, targetPath);
            }
            NatAssetEditorUtil.WriteNatAssetMainfestToFile(Path.Combine(NatAssetEditorUtil.ReadOnlyPath, NatAssetEditorUtil.ConfigName), resultManifest);
        }
    }
}