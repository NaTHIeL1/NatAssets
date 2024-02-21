using System;
using System.Collections.Generic;
using NATFrameWork.NatAsset.Runtime;
using NATFrameWork.Profiler;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace NATFrameWork.NatAsset.Editor
{
    public class UnLoadTaskTreeView : TreeView
    {
        public UnLoadTaskTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            customFoldoutYOffset = 3f;
            Reload();
        }

        public UnLoadTaskTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
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
            var root = new TreeViewItem<UnLoadTaskInfo>(0, -1, "NatAssetUnLoadTaskRoot", new UnLoadTaskInfo());
            List<UnLoadTaskInfo> loadTaskInfos = ProfilerInfo.GetUnLoadTaskInfos();
            if (loadTaskInfos == null || loadTaskInfos.Count == 0)
            {
                root.AddChild(new TreeViewItem<UnLoadTaskInfo>(1, 0, "NoData", new UnLoadTaskInfo()
                {
                    TaskName = "NoData"
                }));
            }

            for (int i = 0; i < loadTaskInfos.Count; i++)
            {
                UnLoadTaskInfo loadTaskInfo = loadTaskInfos[i];
                root.AddChild(new TreeViewItem<UnLoadTaskInfo>(1, 0, loadTaskInfo.TaskName, loadTaskInfo));
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
            TreeViewItem<UnLoadTaskInfo> item = (TreeViewItem<UnLoadTaskInfo>) args.item;
            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                CellGUI(args.GetCellRect(i), item, (UnLoadTaskInfoType) args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<UnLoadTaskInfo> item, UnLoadTaskInfoType taskType, ref RowGUIArgs args)
        {
            UnLoadTaskInfo data = item.data;
            CenterRectUsingSingleLineHeight(ref cellRect);
            switch (taskType)
            {
                case UnLoadTaskInfoType.TaskType:
                    Rect typeRect = cellRect;
                    EditorGUI.LabelField(typeRect, data.TaskType.ToString());
                    break;
                case UnLoadTaskInfoType.TaskName:
                    Rect nameRect = cellRect;
                    EditorGUI.LabelField(nameRect, data.TaskName);
                    break;
                case UnLoadTaskInfoType.TaskState:
                    Rect taskStateRect = cellRect;
                    EditorGUI.LabelField(taskStateRect, data.TaskState.ToString());
                    break;
                case UnLoadTaskInfoType.TaskProcess:
                    Rect taskProcessRect = cellRect;
                    EditorGUI.LabelField(taskProcessRect, data.TaskProcess.ToString("0.00"));
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
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 40,
                    minWidth = 20, maxWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("UnLoadTaskName"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 220,
                    minWidth = 50, maxWidth = 350,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("State"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 40,
                    minWidth = 10, maxWidth = 90,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Process"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 50,
                    minWidth = 10, maxWidth = 90,
                    autoResize = false,
                    allowToggleVisibility = false,
                },
            };
            Assert.AreEqual(colums.Length, Enum.GetValues(typeof(UnLoadTaskInfoType)).Length);
            var state = new MultiColumnHeaderState(colums);
            return state;
        }
    }

    enum UnLoadTaskInfoType
    {
        TaskType,
        TaskName,
        TaskState,
        TaskProcess,
    }
}