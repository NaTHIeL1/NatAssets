using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using NATFrameWork.NatAsset.Runtime;

namespace NATFrameWork.NatAsset.Editor
{
    internal class PreBundleTreeView : TreeView
    {
        internal enum GroupViewType
        {
            BundleName,
            Num,
            Object,
            BundleGroup,
            Encrypt,
            Size,
        }

        private List<BundleBuildInfo> bundleBuildInfos = null;

        internal int GetBundleBuildInfoNum()
        {
            if (bundleBuildInfos == null) return 0;
            return bundleBuildInfos.Count;
        }

        internal int GetAssetsBuildInfoNum()
        {
            if (bundleBuildInfos == null) return 0;
            int assetName = 0;
            for (int i = 0; i < bundleBuildInfos.Count; i++)
            {
                assetName += bundleBuildInfos[i].AssetBuildInfos.Count;
            }

            return assetName;
        }

        internal void RefreshBundleInventories(List<BundleBuildInfo> bundleIns)
        {
            bundleBuildInfos = bundleIns;
            NatAssetEditorUtil.TempBundleBuildInfos = bundleBuildInfos;
            NatAssetEditorUtil.SetSettingDirty();
        }

        public PreBundleTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state,
            multiColumnHeader)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem<BundleBuildInfo>(0, -1, "RootAssetInventory",
                new BundleBuildInfo("NoData", "NoData", "NoData", EditorBundleEncrypt.Nono));
            if (bundleBuildInfos == null)
            {
                bundleBuildInfos = NatAssetEditorUtil.TempBundleBuildInfos;
            }

            if (bundleBuildInfos == null || bundleBuildInfos.Count == 0)
            {
                root.AddChild(new TreeViewItem<BundleBuildInfo>(1, 0, "NoData",
                    new BundleBuildInfo("NoData", "NoData", "NoData", EditorBundleEncrypt.Nono)));
            }
            else
            {
                int index = 1;
                foreach (BundleBuildInfo buildInfo in bundleBuildInfos)
                {
                    TreeViewItem<BundleBuildInfo> bundleTreeItem = new TreeViewItem<BundleBuildInfo>(index++, 0,
                        buildInfo.BundlePath,
                        buildInfo);
                    root.AddChild(bundleTreeItem);

                    foreach (AssetBuildInfo assetBuildInfo in buildInfo.AssetBuildInfos)
                    {
                        TreeViewItem<AssetBuildInfo> assetTreeItem =
                            new TreeViewItem<AssetBuildInfo>(index++, 1, assetBuildInfo.EditorPath, assetBuildInfo);
                        bundleTreeItem.AddChild(assetTreeItem);
                    }
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                DefaultStyles.backgroundOdd.Draw(rect, false, false, false, false);
            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is TreeViewItem<BundleBuildInfo>)
            {
                TreeViewItem<BundleBuildInfo> item = (TreeViewItem<BundleBuildInfo>) args.item;
                for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    CellBundleGUI(args.GetCellRect(i), item, (GroupViewType) args.GetColumn(i), ref args);
                }
            }
            else if (args.item is TreeViewItem<AssetBuildInfo>)
            {
                for (int i = 0; i < args.GetNumVisibleColumns(); i++)
                {
                    TreeViewItem<AssetBuildInfo> item = (TreeViewItem<AssetBuildInfo>) args.item;
                    CellAssetGUI(args.GetCellRect(i), item, (GroupViewType) args.GetColumn(i), ref args);
                }
            }
        }

        protected void OnSortingChanged(MultiColumnHeader header)
        {
            if (header.sortedColumnIndex == -1)
            {
                return;
            }

            bool ascending = header.IsSortedAscending(header.sortedColumnIndex);
            GroupViewType column = (GroupViewType) header.sortedColumnIndex;
            IOrderedEnumerable<BundleBuildInfo> ordered = null;

            switch (column)
            {
                case GroupViewType.BundleName:
                    ordered = bundleBuildInfos.Order(info => info.BundlePath, ascending);
                    break;
                case GroupViewType.Num:
                    ordered = bundleBuildInfos.Order(info => info.AssetBuildInfos.Count, ascending);
                    break;
                case GroupViewType.BundleGroup:
                    ordered = bundleBuildInfos.Order(info => info.BundleGroup, ascending);
                    break;
                case GroupViewType.Encrypt:
                    ordered = bundleBuildInfos.Order(info => info.BundleEncrypt, ascending);
                    break;
                case GroupViewType.Size:
                    ordered = bundleBuildInfos.Order(info => info.Length, ascending);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (ordered != null)
            {
                bundleBuildInfos = new List<BundleBuildInfo>(ordered);
                Reload();
                Repaint();
            }
        }

        void CellBundleGUI(Rect cellRect, TreeViewItem<BundleBuildInfo> item, GroupViewType assetType,
            ref RowGUIArgs args)
        {
            BundleBuildInfo data = item.data;
            CenterRectUsingSingleLineHeight(ref cellRect);
            
            GUIStyle centerStyle = new GUIStyle() {alignment = TextAnchor.MiddleCenter};
            centerStyle.normal = new GUIStyleState() {textColor = Color.white};
            
            GUIStyle rightStyle = new GUIStyle() {alignment = TextAnchor.MiddleRight};
            rightStyle.normal = new GUIStyleState() {textColor = Color.white};
            switch (assetType)
            {
                case GroupViewType.BundleName:
                    Rect bundleNameRect = cellRect;
                    bundleNameRect.x = 20f;
                    EditorGUI.LabelField(bundleNameRect, data.BundlePath);
                    break;
                case GroupViewType.Num:
                    EditorGUI.LabelField(cellRect, data.AssetBuildInfos.Count.ToString(), centerStyle);
                    break;
                case GroupViewType.Object:
                    EditorGUI.BeginDisabledGroup(true);
                    if (data.DirectoryObj == null && !string.IsNullOrEmpty(data.EditorPath))
                    {
                        data.DirectoryObj =
                            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(data.EditorPath);
                    }

                    EditorGUI.ObjectField(cellRect, data.DirectoryObj, typeof(UnityEngine.Object), false);
                    EditorGUI.EndDisabledGroup();
                    break;
                case GroupViewType.BundleGroup:
                    EditorGUI.BeginDisabledGroup(true);
                    data.BundleGroup = EditorGUI.TextField(cellRect, data.BundleGroup);
                    EditorGUI.EndDisabledGroup();
                    break;
                case GroupViewType.Encrypt:
                    EditorGUI.BeginDisabledGroup(true);
                    data.BundleEncrypt = (EditorBundleEncrypt) EditorGUI.EnumPopup(cellRect, data.BundleEncrypt);
                    EditorGUI.EndDisabledGroup();
                    break;
                case GroupViewType.Size:
                    EditorGUI.LabelField(cellRect, FormatSize(data.Length), rightStyle);
                    break;
            }
        }

        void CellAssetGUI(Rect cellRect, TreeViewItem<AssetBuildInfo> item, GroupViewType assetType,
            ref RowGUIArgs args)
        {
            AssetBuildInfo data = item.data;
            CenterRectUsingSingleLineHeight(ref cellRect);
            
            GUIStyle rightStyle = new GUIStyle() {alignment = TextAnchor.MiddleRight};
            rightStyle.normal = new GUIStyleState() {textColor = Color.white};
            
            switch (assetType)
            {
                case GroupViewType.BundleName:
                    Rect bundleNameRect = cellRect;
                    bundleNameRect.x = 30f;
                    EditorGUI.LabelField(bundleNameRect, data.EditorPath);
                    break;
                case GroupViewType.Object:
                    EditorGUI.BeginDisabledGroup(true);
                    if (data.DirectoryObj == null && !string.IsNullOrEmpty(data.EditorPath))
                    {
                        data.DirectoryObj =
                            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(data.EditorPath);
                    }

                    EditorGUI.ObjectField(cellRect, data.DirectoryObj, typeof(UnityEngine.Object), false);
                    EditorGUI.EndDisabledGroup();
                    break;
                case GroupViewType.BundleGroup:
                    break;
                case GroupViewType.Encrypt:
                    break;
                case GroupViewType.Size:
                    EditorGUI.LabelField(cellRect, FormatSize(data.Length), rightStyle);
                    break;
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var colums = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.BundleName.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 450,
                    minWidth = 300, maxWidth = 700,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.Num.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 45,
                    minWidth = 40, maxWidth = 50,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.Object.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 200,
                    minWidth = 50, maxWidth = 300,
                    autoResize = false,
                    allowToggleVisibility = false,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.BundleGroup.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 150,
                    minWidth = 60, maxWidth = 300,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.Encrypt.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 100,
                    minWidth = 40, maxWidth = 200,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(GroupViewType.Size.ToString()),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 100,
                    minWidth = 30, maxWidth = 200,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
            };
            Assert.AreEqual(colums.Length, Enum.GetValues(typeof(GroupViewType)).Length);
            var state = new MultiColumnHeaderState(colums);
            return state;
        }

        private string FormatSize(long originSize)
        {
            string Caluate(long length, float sw)
            {
                return (length * 1.0f / sw).ToString("F2");
            }
            if (originSize < 1000)
            {
                return $"{Caluate(originSize,1)} B";
            }
            else if (originSize < 1000000)
            {
                return $"{Caluate(originSize,1000)} K";
            }
            else if (originSize < 1000000000)
            {
                return $"{Caluate(originSize,1000000)} M";
            }
            else
            {
                return $"{Caluate(originSize,1000000000)} G";
            }
        }
    }
}