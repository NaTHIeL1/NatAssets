using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public enum EditorResourceUpdateMode { Automatic, Manual }

    [Serializable]
    public sealed class EditorResourcePreference
    {
        public ValidBuildTarget Platform;
        public EditorResourceUpdateMode Mode;
        public string SelectedDirectory;
    }

    [FilePath("UserSettings/NatAssetEditorResources.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NatAssetEditorResourceSettings : ScriptableSingleton<NatAssetEditorResourceSettings>
    {
        [SerializeField] private List<EditorResourcePreference> _platforms = new List<EditorResourcePreference>();

        public EditorResourcePreference Read(ValidBuildTarget platform)
        {
            var saved = _platforms.Find(item => item.Platform == platform);
            return saved == null ? new EditorResourcePreference { Platform = platform } :
                JsonUtility.FromJson<EditorResourcePreference>(JsonUtility.ToJson(saved));
        }

        public void Write(EditorResourcePreference preference)
        {
            _platforms.RemoveAll(item => item.Platform == preference.Platform);
            _platforms.Add(JsonUtility.FromJson<EditorResourcePreference>(JsonUtility.ToJson(preference)));
            Save(true);
        }

        public void ImportLegacy(ValidBuildTarget platform, bool automatic, string selectedDirectory)
        {
            if (_platforms.Any(item => item.Platform == platform)) return;
            Write(new EditorResourcePreference { Platform = platform,
                Mode = automatic ? EditorResourceUpdateMode.Automatic : EditorResourceUpdateMode.Manual,
                SelectedDirectory = selectedDirectory });
        }
    }

    /// <summary>One owner across both windows and across script reloads. Nested calls cannot release their owner.</summary>
    public static class NatAssetBuildAccess
    {
        private const string OwnerKey = "NatAsset.BuildOwner";
        public static string Owner => SessionState.GetString(OwnerKey, "");
        public static bool IsBusy => !string.IsNullOrEmpty(Owner);
        public static void RequireAccess(string owner = null)
        {
            if (IsBusy && Owner != owner) throw new InvalidOperationException("已有构建任务占用资源，请等待任务结束。");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("播放、编译或导入期间不能替换或构建资源。");
        }
        public static IDisposable Acquire(string owner)
        {
            if (string.IsNullOrEmpty(owner)) throw new ArgumentException("构建拥有者不能为空。", nameof(owner));
            if (IsBusy && Owner != owner) throw new InvalidOperationException("已有构建任务占用资源。");
            bool acquired = !IsBusy;
            SessionState.SetString(OwnerKey, owner);
            return new Lease(acquired ? owner : null);
        }
        public static void Release(string owner)
        {
            if (!string.IsNullOrEmpty(owner) && Owner == owner) SessionState.EraseString(OwnerKey);
        }
        private sealed class Lease : IDisposable
        {
            private string _owner;
            public Lease(string owner) { _owner = owner; }
            public void Dispose() { Release(_owner); _owner = null; }
        }
    }

    public static class NatAssetEditorResources
    {
        public static IEnumerable<string> Versions(ValidBuildTarget platform)
        {
            string directory = Path.Combine(NatAssetEditorUtil.GetEditorPath, platform.ToString());
            if (!Directory.Exists(directory)) return Array.Empty<string>();
            // Full checks happen on apply; listings only read manifests, so repaint never hashes all bundles.
            var versions = new List<KeyValuePair<DateTime, string>>();
            foreach (string candidate in Directory.GetDirectories(directory))
            {
                try
                {
                    if (!DateTime.TryParseExact(Path.GetFileName(candidate), "yyyy-MM-dd-HH-mm",
                        System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time)) continue;
                    var manifest = NatAssetManifest.DeserializeFromBinary(File.ReadAllBytes(Path.Combine(candidate, NatAssetSetting.ConfigName)));
                    if (manifest.Platform == platform.ToString() && manifest.BundleManifests?.Count > 0)
                        versions.Add(new KeyValuePair<DateTime, string>(time, candidate));
                }
                catch (Exception) { /* Partial/corrupt outputs are not selectable versions. */ }
            }
            return versions.OrderByDescending(item => item.Key).Select(item => item.Value).ToArray();
        }

        public static void OnResourcesBuilt(ResourceBuildResult result, string owner)
        {
            var target = (ValidBuildTarget)Enum.Parse(typeof(ValidBuildTarget), result.Platform);
            if (NatAssetEditorResourceSettings.instance.Read(target).Mode == EditorResourceUpdateMode.Manual)
            { result.EditorSyncStatus = "手动指定版本：保持当前编辑器测试资源。"; return; }
            try
            {
                var applied = EditorResourceSync.Apply(result.Directory, (BuildTarget)target, owner);
                result.EditorSyncStatus = "编辑器测试资源已同步：" + applied.version;
                result.EditorSyncWarning = applied.warning;
            }
            catch (Exception error)
            {
                result.EditorSyncWarning = "资源构建成功，编辑器资源同步失败：" + error.Message;
                Debug.LogWarning("[NatAsset] " + result.EditorSyncWarning);
            }
        }
    }
}
