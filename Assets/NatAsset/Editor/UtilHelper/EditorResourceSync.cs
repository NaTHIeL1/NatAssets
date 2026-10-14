using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NATFrameWork.NatAsset.Editor;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public sealed class EditorResourceSelection
    {
        public string sourceDirectory, version, platform, appliedUtc, manifestHash, warning;
    }

    /// <summary>Transactional publication to NatAsset's existing editor bundle directory.</summary>
    [InitializeOnLoad]
    public static class EditorResourceSync
    {
        private const string SelectionName = "nat-editor-resource.json";
        public static bool IsRunning { get; private set; }
        [Serializable] private sealed class Journal { public bool originalExisted, committed; }
        static EditorResourceSync() { EditorApplication.delayCall += RecoverAll; }

        public static string GetDirectory(BuildTarget target) => Path.GetFullPath(Path.Combine(
            NatAssetEditorUtil.GetEditorPath, target.ToString(), NatAssetEditorUtil.SPECIAL_EditorFile));

        public static EditorResourceSelection ReadApplied(BuildTarget target)
        {
            string directory = GetDirectory(target);
            var result = NatAssetEditorFiles.ReadJson<EditorResourceSelection>(Path.Combine(directory, SelectionName));
            if (result == null) return null;
            string manifest = Path.Combine(directory, NatAssetSetting.ConfigName);
            var journal = NatAssetEditorFiles.ReadJson<Journal>(directory + ".sync.json");
            if ((journal != null && !journal.committed) || !File.Exists(manifest) ||
                NatAssetEditorFiles.HashFile(manifest) != result.manifestHash)
                throw new InvalidDataException("编辑器测试目录已变化或同步尚未恢复，请重新应用资源版本。");
            return result;
        }

        public static EditorResourceSelection Apply(string source, BuildTarget target, string owner = null)
        {
            NatAssetBuildAccess.RequireAccess(owner);
            if (IsRunning) throw new InvalidOperationException("编辑器资源正在同步。");
            if (owner == null && EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException($"请先将 Unity 当前平台切换到 {target}。");
            NatAssetBuildService.InspectResourceVersion(source, (ValidBuildTarget)target);
            IsRunning = true;
            try { return Synchronize(source, GetDirectory(target), (ValidBuildTarget)target); }
            finally { IsRunning = false; }
        }

        // Separate filesystem operation also used by failure-injection tests.
        public static EditorResourceSelection Synchronize(string source, string destination, ValidBuildTarget target, Action<string> checkpoint = null)
        {
            source = Path.GetFullPath(source);
            destination = Path.GetFullPath(destination);
            string parent = Path.GetDirectoryName(destination);
            foreach (string path in new[] { destination, destination + ".pending", destination + ".backup", destination + ".sync.json" })
                NatAssetEditorFiles.RequireChild(path, parent);
            if (source.Equals(destination, StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("资源来源与编辑器目录不能重叠。");
            Recover(destination);
            var result = NatAssetBuildService.InspectResourceVersion(source, target);
            string pending = destination + ".pending", backup = destination + ".backup", journalPath = destination + ".sync.json";
            if (Directory.Exists(pending) || Directory.Exists(backup)) throw new IOException("存在未识别的同步暂存目录，请先检查：" + destination);
            var journal = new Journal { originalExisted = Directory.Exists(destination) };
            NatAssetEditorFiles.WriteJson(journalPath, journal);
            var selection = new EditorResourceSelection
            {
                sourceDirectory = source, version = result.Version, platform = result.Platform,
                appliedUtc = DateTime.UtcNow.ToString("O"), manifestHash = NatAssetEditorFiles.HashFile(Path.Combine(source, NatAssetSetting.ConfigName))
            };
            try
            {
                NatAssetBuildService.CopyResourceVersion(source, pending, target);
                var oldManaged = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { NatAssetSetting.ConfigName, NatAssetSetting.ConfigName + ".json", SelectionName };
                string oldManifestPath = Path.Combine(destination, NatAssetSetting.ConfigName);
                if (File.Exists(oldManifestPath))
                {
                    try
                    {
                        var old = NatAssetManifest.DeserializeFromBinary(File.ReadAllBytes(oldManifestPath));
                        if (old?.BundleManifests != null)
                            foreach (var bundle in old.BundleManifests) oldManaged.Add(bundle.RelativePath.Replace('\\', '/'));
                    }
                    catch (Exception) { /* Unknown old files are preserved when the old manifest cannot identify ownership. */ }
                }
                if (journal.originalExisted) CopyUnmanaged(destination, destination, pending, oldManaged);
                NatAssetEditorFiles.WriteJson(Path.Combine(pending, SelectionName), selection);
                NatAssetBuildService.InspectResourceVersion(pending, target);
                checkpoint?.Invoke("Prepared");
                if (journal.originalExisted) Directory.Move(destination, backup);
                checkpoint?.Invoke("BackedUp");
                Directory.Move(pending, destination);
                checkpoint?.Invoke("Published");
                journal.committed = true;
                NatAssetEditorFiles.WriteJson(journalPath, journal);
            }
            catch
            {
                Recover(destination);
                throw;
            }
            // A cleanup failure leaves a committed journal; recovery keeps the published version.
            try { Recover(destination); }
            catch (Exception error)
            {
                selection.warning = "测试版本已生效，备份清理待恢复：" + error.Message;
                Debug.LogWarning("[NatBuild] " + selection.warning);
            }
            return selection;
        }

        private static void CopyUnmanaged(string root, string directory, string output, HashSet<string> managed)
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                NatAssetEditorFiles.RequireChild(file, root);
                string relative = file.Substring(root.TrimEnd('\\', '/').Length + 1).Replace('\\', '/');
                if (managed.Contains(relative)) continue;
                string target = Path.Combine(output, relative);
                NatAssetEditorFiles.RequireChild(target, output);
                if (File.Exists(target)) throw new IOException("新的 Bundle 与非托管文件冲突：" + relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target);
            }
            foreach (string child in Directory.GetDirectories(directory))
            {
                NatAssetEditorFiles.RequireChild(child, root);
                string target = Path.Combine(output, child.Substring(root.TrimEnd('\\', '/').Length + 1));
                Directory.CreateDirectory(target);
                CopyUnmanaged(root, child, output, managed);
            }
        }

        public static void Recover(string destination)
        {
            destination = Path.GetFullPath(destination);
            string parent = Path.GetDirectoryName(destination);
            string journalPath = destination + ".sync.json", pending = destination + ".pending", backup = destination + ".backup";
            foreach (string path in new[] { destination, pending, backup, journalPath }) NatAssetEditorFiles.RequireChild(path, parent);
            var journal = NatAssetEditorFiles.ReadJson<Journal>(journalPath);
            if (journal == null) return;
            if (!journal.committed)
            {
                if (Directory.Exists(backup))
                {
                    DeleteOwnedDirectory(destination, parent);
                    Directory.Move(backup, destination);
                }
                else if (!journal.originalExisted) DeleteOwnedDirectory(destination, parent);
                else if (!Directory.Exists(destination)) throw new IOException("编辑器原资源及备份均缺失：" + destination);
            }
            DeleteOwnedDirectory(pending, parent);
            DeleteOwnedDirectory(backup, parent);
            File.Delete(journalPath);
        }
        private static void DeleteOwnedDirectory(string directory, string parent)
        {
            NatAssetEditorFiles.RequireChild(directory, parent);
            if (!Directory.Exists(directory)) return;
            // Reject links at every level before recursive deletion.
            foreach (string child in Directory.GetDirectories(directory)) DeleteOwnedDirectory(child, directory);
            foreach (string file in Directory.GetFiles(directory)) { NatAssetEditorFiles.RequireChild(file, directory); File.Delete(file); }
            Directory.Delete(directory);
        }
        private static void RecoverAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || NatAssetBuildAccess.IsBusy) return;
            foreach (var target in new[] { BuildTarget.StandaloneWindows64, BuildTarget.Android, BuildTarget.iOS, BuildTarget.WebGL })
                try { Recover(GetDirectory(target)); }
                catch (Exception error) { Debug.LogError("[NatBuild] 编辑器测试资源恢复失败：" + error); }
        }
    }
}
