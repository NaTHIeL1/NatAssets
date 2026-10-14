using System;
using System.IO;
using System.Linq;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    internal sealed class EditorResourcesView
    {
        private ValidBuildTarget _target = ValidBuildTarget.StandaloneWindows64;
        private string[] _versions = Array.Empty<string>();
        private bool _initialized;
        private string _message;
        public void Draw()
        {
            if (!_initialized)
            {
                _initialized = true;
                if (Enum.IsDefined(typeof(ValidBuildTarget), (int)EditorUserBuildSettings.activeBuildTarget))
                    _target = (ValidBuildTarget)EditorUserBuildSettings.activeBuildTarget;
                Refresh();
            }
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("编辑器 Bundle 测试资源", EditorStyles.boldLabel);
                var target = (ValidBuildTarget)EditorGUILayout.EnumPopup("测试资源平台", _target);
                if (target != _target) { _target = target; _message = null; Refresh(); }
                var preference = NatAssetEditorResourceSettings.instance.Read(_target);
                bool busy = NatAssetBuildAccess.IsBusy || EditorResourceSync.IsRunning || EditorApplication.isPlayingOrWillChangePlaymode ||
                    EditorApplication.isCompiling || EditorApplication.isUpdating;
                using (new EditorGUI.DisabledScope(busy))
                {
                    EditorGUI.BeginChangeCheck();
                    preference.Mode = (EditorResourceUpdateMode)EditorGUILayout.Popup("更新模式", (int)preference.Mode,
                        new[] { "自动跟随资源构建", "手动指定版本" });
                    int index = Array.IndexOf(_versions, preference.SelectedDirectory);
                    var labels = new[] { "选择资源版本…" }.Concat(_versions.Select(Path.GetFileName)).ToArray();
                    int chosen = EditorGUILayout.Popup("测试资源版本", index + 1, labels) - 1;
                    if (chosen >= 0 && chosen != index) preference.SelectedDirectory = _versions[chosen];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        preference.SelectedDirectory = EditorGUILayout.TextField("资源目录", preference.SelectedDirectory ?? "");
                        if (GUILayout.Button("选择…", GUILayout.Width(70)))
                        {
                            string path = EditorUtility.OpenFolderPanel("选择编辑器测试资源", NatAssetEditorUtil.GetEditorPath, "");
                            if (!string.IsNullOrEmpty(path)) { preference.SelectedDirectory = path; GUI.changed = true; }
                        }
                    }
                    if (EditorGUI.EndChangeCheck()) NatAssetEditorResourceSettings.instance.Write(preference);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("刷新版本列表")) Refresh();
                        if (GUILayout.Button("应用到编辑器测试"))
                        {
                            try
                            {
                                var applied = EditorResourceSync.Apply(preference.SelectedDirectory, (BuildTarget)_target);
                                _message = "已应用：" + applied.version + " " + applied.warning;
                            }
                            catch (Exception error) { _message = error.Message; }
                        }
                    }
                }
                EditorGUILayout.LabelField(preference.Mode == EditorResourceUpdateMode.Automatic
                    ? "下一次资源构建成功后自动更新；仅复用资源不更新。主包后续失败不撤销本次同步。"
                    : "构建不会替换当前版本；选择资源后点击应用。", EditorStyles.wordWrappedMiniLabel);
                try
                {
                    var applied = EditorResourceSync.ReadApplied((BuildTarget)_target);
                    EditorGUILayout.LabelField(applied == null ? "尚未应用测试资源" :
                        $"当前生效：{applied.version} | {applied.platform}\n来源：{applied.sourceDirectory}", EditorStyles.wordWrappedMiniLabel);
                }
                catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Warning); }
                if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
                EditorGUILayout.LabelField("Package Only 使用所选 Bundle；执行的 C# 仍是当前工程代码。", EditorStyles.wordWrappedMiniLabel);
            }
        }
        public void Refresh() { _versions = NatAssetEditorResources.Versions(_target).ToArray(); }
    }
}
