using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class PreBundleView : ISubView
    {
        private BuildBundleWindow _buildBundleWindow;
        private TreeViewState _treeViewStateGroup;
        private MultiColumnHeaderState _multiColumnHeaderState;
        private PreBundleTreeView _preBundleTreeView;
        private SearchField _searchField;
        public void EditorWindow(EditorWindow editorWindow)
        {
            _buildBundleWindow = editorWindow as BuildBundleWindow;
        }

        protected List<string> GetColumns()
        {
            List<string> columnList = new List<string>()
            {
                "BundleName",
                "Object",
                "BundleGroup",
                "Encrypt",
                //"删除",
            };

            return columnList;
        }

        public void OnEnable()
        {
            _treeViewStateGroup = _treeViewStateGroup == null ? new TreeViewState() : _treeViewStateGroup;
            MultiColumnHeaderState headerState = PreBundleTreeView.CreateDefaultMultiColumnHeaderState(100);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_multiColumnHeaderState, headerState))
            {
                MultiColumnHeaderState.OverwriteSerializedFields(_multiColumnHeaderState, headerState);
            }
            _multiColumnHeaderState = headerState;
            MultiColumnHeader groupHeaderState = new MultiColumnHeader(headerState);
            groupHeaderState.ResizeToFit();

            _preBundleTreeView = new PreBundleTreeView(_treeViewStateGroup, groupHeaderState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _preBundleTreeView.SetFocusAndEnsureSelectedItem;
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            _preBundleTreeView.searchString = _searchField.OnToolbarGUI(_preBundleTreeView.searchString);
            if (GUILayout.Button("Refresh"))
            {
                _preBundleTreeView.RefreshBundleInventories(NatAssetBuildUtil.CollectBundles());
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"BundleNum:{_preBundleTreeView.GetBundleBuildInfoNum()}", new[] { GUILayout.Width(150) });
            GUILayout.Label($"AssetNum:{_preBundleTreeView.GetAssetsBuildInfoNum()}", new[] { GUILayout.Width(150) });
            GUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);
            _preBundleTreeView.OnGUI(rect);
            _preBundleTreeView.Reload();
            _preBundleTreeView.Repaint();
        }
    }
}