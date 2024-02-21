using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class VirtualFileTreeView : TreeView
    {
        private Action<IVirtualFile> _action;

        protected override void SingleClickedItem(int id)
        {
            TreeViewItem<IVirtualFile> viewItem = (TreeViewItem<IVirtualFile>) FindItem(id, rootItem);
            _action?.Invoke(viewItem.data);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        internal void ListenSelectChanged(Action<IVirtualFile> callback)
        {
            _action = callback;
        }

        public VirtualFileTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            customFoldoutYOffset = 3f;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem<IVirtualFile> root =
                new TreeViewItem<IVirtualFile>(0, -1, "RootVirtualFile", new VirtualFolder());
            List<PackageVirtualRoot> virtualRoots = VirtualFileUtil.VirtualFiles;
            if (virtualRoots == null || virtualRoots.Count == 0)
            {
                root.AddChild(new TreeViewItem<IVirtualFile>(1, 0, "Nono",
                    new VirtualFile("Nono", "Nono", "nono", null)));
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            //构建虚拟文件树
            VirtualFileUtil.BuildVirtualFile();
            //构建虚拟文件treeviewItem
            VirtualFileUtil.BuildVirtualFileTreeView(root);
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
            //base.RowGUI(args);
            float contentIndent = GetContentIndent(args.item);
            Rect rowRect = args.rowRect;
            Rect rect = args.rowRect;
            rect.x = contentIndent;
            TreeViewItem<IVirtualFile> item = (TreeViewItem<IVirtualFile>) args.item;
            IVirtualFile data = item.data;
            if (data is VirtualFolder)
            {
                DrawFolder(rect, item);
            }
            else if (data is VirtualFile)
            {
                DrawFile(rect, item);
            }
            else if (data is PackageVirtualRoot)
            {
                PackageVirtualRoot packageVirtualData = (PackageVirtualRoot) data;
                if (packageVirtualData.IsDirectory)
                {
                    DrawRootFolder(rect, item);
                }
                else
                {
                    DrawRootFile(rect, item);
                }
            }

            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y + rowRect.height, rowRect.width, 1f), Color.black);
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return 20f;
        }

        private void DrawFolder(Rect rect, TreeViewItem<IVirtualFile> viewItem)
        {
            IVirtualFile virtualFile = viewItem.data;
            Rect tempRect = rect;
            tempRect.width = 25f;
            GUI.DrawTexture(tempRect, virtualFile.GetIcon, ScaleMode.ScaleToFit);
            tempRect.x += 25;
            tempRect.width = rect.width - 25;
            EditorGUI.LabelField(tempRect, virtualFile.Name);
        }

        private void DrawFile(Rect rect, TreeViewItem<IVirtualFile> viewItem)
        {
            IVirtualFile virtualFile = viewItem.data;
            Rect tempRect = rect;
            tempRect.width = 25f;
            GUI.DrawTexture(tempRect, virtualFile.GetIcon, ScaleMode.ScaleToFit);
            tempRect.x += 25;
            tempRect.width = rect.width - 25;
            EditorGUI.LabelField(tempRect, virtualFile.Name);
        }

        private void DrawRootFolder(Rect rect, TreeViewItem<IVirtualFile> viewItem)
        {
            IVirtualFile virtualFile = viewItem.data;
            Rect tempRect = rect;
            tempRect.width = 25f;
            GUI.DrawTexture(tempRect, virtualFile.GetIcon, ScaleMode.ScaleToFit);
            tempRect.x += 25;
            tempRect.width = rect.width - 25;
            EditorGUI.LabelField(tempRect, virtualFile.FileName);

            Color bc = GUI.backgroundColor;
            GUI.backgroundColor = new Color(255 / 255, 35 / 255, 35 / 255);
            Rect delBtnRect = new Rect(rect.width - 50, rect.y, 50, rect.height);
            if (GUI.Button(delBtnRect, "Delete"))
            {
                if (EditorUtility.DisplayDialog("Tip", "Delete current package?", "Yes", "No"))
                {
                    VirtualFileUtil.RemovePackageVirtualRoot(virtualFile.FileName);
                    NatAssetEditorUtil.SetSettingDirty();
                    Refresh();
                    Debug.Log($"Package:{virtualFile.FileName}已删除");
                }
            }

            GUI.backgroundColor = bc;

            if (virtualFile.Child == null)
            {
                float boxLength = 120;
                Rect warRect = new Rect(delBtnRect.x - boxLength, delBtnRect.y, boxLength, delBtnRect.height);
                EditorGUI.HelpBox(warRect, "Package is Null", MessageType.Warning);
            }
        }

        private void DrawRootFile(Rect rect, TreeViewItem<IVirtualFile> viewItem)
        {
            IVirtualFile virtualFile = viewItem.data;
            Rect tempRect = rect;
            tempRect.width = 25f;
            GUI.DrawTexture(tempRect, virtualFile.GetIcon, ScaleMode.ScaleToFit);
            tempRect.x += 25;
            tempRect.width = rect.width - 25;
            EditorGUI.LabelField(tempRect, virtualFile.FileName);

            Color bc = GUI.backgroundColor;
            GUI.backgroundColor = new Color(255 / 255, 35 / 255, 35 / 255);
            if (GUI.Button(new Rect(rect.width - 50, rect.y, 50, rect.height), "Delete"))
            {
                if (EditorUtility.DisplayDialog("Tip", "Delete current package?", "Yes", "No"))
                {
                    VirtualFileUtil.RemovePackageVirtualRoot(virtualFile.FileName);
                    NatAssetEditorUtil.SetSettingDirty();
                    Refresh();
                    Debug.Log($"Package:{virtualFile.FileName}已删除");
                }
            }

            GUI.backgroundColor = bc;
        }

        private void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                //if(GUILayout.Button())
            }
        }

        private void Refresh()
        {
            Reload();
            Repaint();
        }
    }
}