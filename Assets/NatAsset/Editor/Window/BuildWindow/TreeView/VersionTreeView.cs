using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    internal class VersionTreeView : TreeView
    {
        private Action<VersionInfo> _action;
        public VersionTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            customFoldoutYOffset = 3f;
            Reload();
        }
        internal void ListenSelectChanged(Action<VersionInfo> callback)
        {
            _action = callback;
        }
        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem<VersionInfo> root =
                new TreeViewItem<VersionInfo>(0, -1, "RootVirtualFile", new VersionInfo());
            List<VersionInfo> versionInfos = VersionConfig.GetVersionInfo(NatAssetEditorUtil.EditorTargetPlatform);
            if (versionInfos == null || versionInfos.Count == 0)
            {
                root.AddChild(new TreeViewItem<VersionInfo>(1, 0, "Nono",
                    new VersionInfo() { Name = "Nono" }));
                return root;
            }

            for (int i = 0; i < versionInfos.Count; i++)
            {
                VersionInfo versionInfo = versionInfos[i];
                TreeViewItem<VersionInfo> treeViewItem = new TreeViewItem<VersionInfo>(i + 1, 0, versionInfo.Name, versionInfo);
                root.AddChild(treeViewItem);
            }
            return root;
        }

        protected override void SingleClickedItem(int id)
        {
            TreeViewItem<VersionInfo> viewItem = (TreeViewItem<VersionInfo>)FindItem(id, rootItem);
            _action?.Invoke(viewItem.data);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 20f;
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                DefaultStyles.backgroundOdd.Draw(rect, false, false, false, false);
            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            //base.RowGUI(args);
            float contentIndent = GetContentIndent(args.item);
            Rect rowRect = args.rowRect;
            Rect rect = args.rowRect;
            rect.x = contentIndent;
            TreeViewItem<VersionInfo> item = (TreeViewItem<VersionInfo>)args.item;
            VersionInfo data = item.data;

            DrawVersion(rect, item);
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y + rowRect.height, rowRect.width, 1f), Color.black);
        }

        private void DrawVersion(Rect rect, TreeViewItem<VersionInfo> viewItem)
        {
            VersionInfo versionInfo = viewItem.data;
            Rect tempRect = rect;
            EditorGUI.LabelField(tempRect, $"Version:{versionInfo.Name}");

            if (string.IsNullOrEmpty(versionInfo.FullName)) return;

            Color bc = GUI.backgroundColor;
            GUI.backgroundColor = new Color(255 / 255, 35 / 255, 35 / 255);
            Rect delBtnRect = new Rect(rect.width - 50, rect.y, 50, rect.height);
            if (GUI.Button(delBtnRect, "Delete"))
            {
                if (EditorUtility.DisplayDialog("Tip", "Delete current version?", "Yes", "No"))
                {
                    Directory.Delete(versionInfo.FullName, true);
                    Refresh();
                    Debug.Log($"版本构建文件:{versionInfo.FullName}已删除");
                }
            }

            GUI.backgroundColor = bc;

            Rect openRect = new Rect(delBtnRect.x - 50, rect.y, 50, rect.height);
            if (GUI.Button(openRect, "Open"))
            {
                NatAssetEditorUtil.OpenDirectory(versionInfo.FullName);
            }
        }

        private void Refresh()
        {
            Reload();
            Repaint();
        }
    }
}
