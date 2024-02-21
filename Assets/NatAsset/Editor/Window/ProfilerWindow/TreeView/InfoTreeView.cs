using NATFrameWork.Profiler;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace NATFrameWork.NatAsset.Editor
{
    public class InfoTreeView : TreeView
    {
        public InfoTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            customFoldoutYOffset = 3f;
            Reload();
        }

        public InfoTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        public void Update()
        {
            Reload();
            Repaint();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem<RefInfo>(0, -1, "Root", new RefInfo());
            List<RefInfo> refInfos = ProfilerInfo.GetRefInfos();
            if (refInfos == null || refInfos.Count == 0)
            {
                root.AddChild(new TreeViewItem<RefInfo>(1, 0, "test", new RefInfo()));
            }

            for (int i = 0; i < refInfos.Count; i++)
            {
                RefInfo refInfo = refInfos[i];
                root.AddChild(new TreeViewItem<RefInfo>(1, 0, refInfo.InfoName, refInfo));
            }

            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return base.GetCustomRowHeight(row, item);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                DefaultStyles.backgroundOdd.Draw(rect, false, false, false, false);
            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            TreeViewItem<RefInfo> item = (TreeViewItem<RefInfo>) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, (RefInfoType) args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<RefInfo> item, RefInfoType assetType, ref RowGUIArgs args)
        {
            RefInfo data = item.data;
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (assetType)
            {
                case RefInfoType.InfoType:
                    Rect typeRect = cellRect;
                    EditorGUI.LabelField(typeRect, data.NatAssetType.ToString());
                    break;
                case RefInfoType.InfoName:
                    Rect nameRect = cellRect;
                    EditorGUI.LabelField(nameRect, data.InfoName);
                    break;
                case RefInfoType.InfoRefCount:
                    Rect refCountRect = cellRect;
                    EditorGUI.LabelField(refCountRect, data.RefCount.ToString());
                    break;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var colums = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("InfoType"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 50,
                    minWidth = 20, maxWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("InfoName"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 200,
                    minWidth = 50, maxWidth = 350,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("RefCount"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 60,
                    minWidth = 30, maxWidth = 90,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
            };
            Assert.AreEqual(colums.Length, Enum.GetValues(typeof(RefInfoType)).Length);
            var state = new MultiColumnHeaderState(colums);
            return state;
        }
    }

    enum RefInfoType
    {
        InfoType,
        InfoName,
        InfoRefCount,
    }
}