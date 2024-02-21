using System;
using System.Collections.Generic;
using NATFrameWork.Profiler;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace NATFrameWork.NatAsset.Editor
{
    public class HandleTreeView : TreeView
    {
        public HandleTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            customFoldoutYOffset = 3f;
            Reload();
        }

        public HandleTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
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
            var root = new TreeViewItem<HandleInfo>(0, -1, "HandleInfoRoot", new HandleInfo());
            Dictionary<string, HandleInfo> handleInfos = ProfilerInfo.GetHandleInfos();
            if (handleInfos == null || handleInfos.Count == 0)
            {
                root.AddChild(new TreeViewItem<HandleInfo>(1, 0, "NoData", new HandleInfo()
                {
                    HandleName = "NoData",
                }));
            }

            foreach (KeyValuePair<string, HandleInfo> keyValuePair in handleInfos)
            {
                HandleInfo handleInfo = keyValuePair.Value;
                root.AddChild(new TreeViewItem<HandleInfo>(1, 0, handleInfo.HandleName, handleInfo));
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
            TreeViewItem<HandleInfo> item = (TreeViewItem<HandleInfo>) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, (HandleInfoType) args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<HandleInfo> item, HandleInfoType assetType, ref RowGUIArgs args)
        {
            HandleInfo data = item.data;
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (assetType)
            {
                case HandleInfoType.infoType:
                    Rect typeRect = cellRect;
                    EditorGUI.LabelField(typeRect, data.HandleType.ToString());
                    break;
                case HandleInfoType.infoName:
                    Rect nameRect = cellRect;
                    EditorGUI.LabelField(nameRect, data.HandleName);
                    break;
                // case HandleInfoType.InfoRefCount:
                //     Rect refCountRect = cellRect;
                //     EditorGUI.LabelField(refCountRect, data.RefCount.ToString());
                //     break;
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
                    headerContent = new GUIContent("Type"),
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
            };
            Assert.AreEqual(colums.Length, Enum.GetValues(typeof(RefInfoType)).Length);
            var state = new MultiColumnHeaderState(colums);
            return state;
        }
    }

    enum HandleInfoType
    {
        infoType,
        infoName,
    }
}