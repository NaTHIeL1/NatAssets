using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public class VirtualFileView : ISubView
    {
        private BuildBundleWindow _buildBundleWindow;
        private SearchField _searchField;
        private TreeViewState _virtualTreeViewState;
        private VirtualFileTreeView _virtualFileTreeView;
        private float splitWith = 650f;
        private bool _draging = false;
        private Rect _virtualRect;
        private Rect _splitRect;
        private Rect _rightAddRect;

        //data
        private IVirtualFile _virtualFile;

        public void EditorWindow(EditorWindow editorWindow)
        {
            _buildBundleWindow = (BuildBundleWindow) editorWindow;
        }

        public void OnEnable()
        {
            _virtualTreeViewState = _virtualTreeViewState == null ? new TreeViewState() : _virtualTreeViewState;
            _virtualFileTreeView = new VirtualFileTreeView(_virtualTreeViewState);
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _virtualFileTreeView.SetFocusAndEnsureSelectedItem;

            _virtualFileTreeView.ListenSelectChanged(ListenSelectChanged);
        }

        public void OnGUI()
        {
            DrawToolBar();
            DrawVirtualFileView();
            DrawDragSplitLine();
            DrawAddPackageView();
            DrawBuildSettingView();
            DrawLeftComp();
        }

        private void ListenSelectChanged(IVirtualFile virtualFile)
        {
            _virtualFile = virtualFile;
        }

        private void Refresh()
        {
            _virtualFileTreeView.Reload();
            _virtualFileTreeView.Repaint();
        }

        private void DrawToolBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            _virtualFileTreeView.searchString = _searchField.OnToolbarGUI(_virtualFileTreeView.searchString,
                new[] {GUILayout.Width(_buildBundleWindow.position.width * 0.6f)});
            if (GUILayout.Button("Refresh", new[] {GUILayout.Width(100)}))
            {
                VirtualFileUtil.Sort();
                NatAssetEditorUtil.SetSettingDirty();
                NatAssetBuildSetting.Instance.InitBundleCollector();
                Refresh();
            }

            if (GUILayout.Button("Clear", new[] {GUILayout.Width(100)}))
            {
                VirtualFileUtil.VirtualFiles.Clear();
                NatAssetEditorUtil.SetSettingDirty();
                Refresh();
            }

            //if (GUILayout.Button("Auto", new[] {GUILayout.Width(100)}))
            //{
            //    VirtualFileUtil.RefreshVirtualRoot();
            //    VirtualFileUtil.Sort();
            //    NatAssetEditorUtil.SetSettingDirty();
            //    Refresh();
            //}

            GUILayout.EndHorizontal();
        }

        private void DrawVirtualFileView()
        {
            _virtualRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _virtualRect.width = splitWith;
            _virtualFileTreeView.OnGUI(_virtualRect);
        }

        private void DrawDragSplitLine()
        {
            _splitRect = new Rect(splitWith, _virtualRect.y, 2, _virtualRect.height);
            EditorGUI.DrawRect(_splitRect, Color.gray);
            GUIHelperUtil.DragRectH(ref _splitRect, ref _draging, ref splitWith, _buildBundleWindow.position, 0.4f,
                0.9f);
        }

        private void DrawAddPackageView()
        {
            _rightAddRect = new Rect(_splitRect.x + _splitRect.width, 50,
                _buildBundleWindow.position.width - (_splitRect.x + _splitRect.width), 50);
            GUILayout.BeginArea(_rightAddRect);
            if (GUILayout.Button("AddFolderPackage"))
            {
                string folder = EditorUtility.OpenFolderPanel("选择要添加的目录", "Assets", "");
                if (folder != string.Empty)
                {
                    if (!folder.Contains(Application.dataPath))
                    {
                        Debug.LogError($"目录必须处于项目Assets目录内");
                    }

                    string fileName = folder.Replace(Application.dataPath, "Assets");
                    VirtualFileUtil.AddPackageVirtualRoot(fileName);
                    VirtualFileUtil.Sort();
                    NatAssetEditorUtil.SetSettingDirty();
                    Refresh();
                }
            }

            if (GUILayout.Button("AddFilePackage"))
            {
                string folder = EditorUtility.OpenFilePanel("选择要添加的文件", "Assets", "");
                if (folder != string.Empty)
                {
                    if (!folder.Contains(Application.dataPath))
                    {
                        Debug.LogError($"文件必须处于项目Assets目录内");
                        return;
                    }

                    if (!NatAssetEditorUtil.IsValidAsset(folder))
                    {
                        Debug.LogError("无效资源");
                        return;
                    }

                    string fileName = folder.Replace(Application.dataPath, "Assets");
                    VirtualFileUtil.AddPackageVirtualRoot(fileName);
                    VirtualFileUtil.Sort();
                    NatAssetEditorUtil.SetSettingDirty();
                    Refresh();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawBuildSettingView()
        {
            float top = _rightAddRect.y + _rightAddRect.height;
            Rect buildSettingRect = new Rect(_rightAddRect.x, top, _rightAddRect.width,
                _buildBundleWindow.position.height - top);
            GUILayout.BeginArea(buildSettingRect);
            if (_virtualFile != null)
            {
                if (_virtualFile is IVirtualRoot)
                {
                    float labelWidth = 60f;
                    IVirtualRoot virtualRoot = _virtualFile as IVirtualRoot;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Package:", new[] {GUILayout.Width(labelWidth)});
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.TextArea(_virtualFile.FileName);
                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Bundle:", new[] {GUILayout.Width(labelWidth)});
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.TextArea(
                        NatAssetEditorUtil.FormatBundlePath(
                            _virtualFile.FullName.Replace(Application.dataPath + "/", "")));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.EndHorizontal();

                    EditorGUI.BeginChangeCheck();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Collector:", new[] {GUILayout.Width(labelWidth)});
                    string[] strs = NatAssetEditorUtil.BundleCollectorList;
                    int lastSelect = 0;
                    for (int i = 0; i < strs.Length; i++)
                        if (strs[i] == virtualRoot.CollectorName)
                            lastSelect = i;
                    int selectIndex = EditorGUILayout.Popup(lastSelect, strs);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Encrypt:", new[] {GUILayout.Width(labelWidth)});
                    EditorBundleEncrypt encryptType = (EditorBundleEncrypt)EditorGUILayout.EnumPopup(virtualRoot.EncryptName);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Group:", new[] {GUILayout.Width(labelWidth)});
                    string tempGroup = GUILayout.TextArea(virtualRoot.Group);
                    GUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck())
                    {
                        virtualRoot.EncryptName = encryptType;
                        virtualRoot.CollectorName = strs[selectIndex];
                        virtualRoot.Group = tempGroup;
                        NatAssetEditorUtil.SetSettingDirty();
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawLeftComp()
        {
            GUILayout.BeginHorizontal(new[] {GUILayout.Width(splitWith)});
            if (GUILayout.Button("ExpandAll"))
            {
                _virtualFileTreeView.ExpandAll();
            }

            if (GUILayout.Button("CollapseAll"))
            {
                _virtualFileTreeView.CollapseAll();
            }

            GUILayout.EndHorizontal();
        }
    }
}