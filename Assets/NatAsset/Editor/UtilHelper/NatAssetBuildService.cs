using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public sealed class ResourceBuildResult
    {
        public string Directory;
        public string Version;
        public string Platform;
        public string EditorSyncStatus, EditorSyncWarning;
    }

    /// <summary>Resource builds and manifest-driven staging shared by editor build entry points.</summary>
    public static class NatAssetBuildService
    {
        public static ResourceBuildResult BuildResources(ValidBuildTarget target, string owner = null)
        {
            return BuildResources(target, owner, syncEditorResources: true);
        }

        /// <summary>保留原构建流程，由调用方决定本次是否自动同步编辑器测试资源。</summary>
        public static ResourceBuildResult BuildResources(ValidBuildTarget target, string owner, bool syncEditorResources)
        {
            ValidateTarget(target);
            NatAssetBuildAccess.RequireAccess(owner);
            owner = owner ?? "NatAsset-" + Guid.NewGuid().ToString("N");
            using (NatAssetBuildAccess.Acquire(owner))
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    throw new InvalidOperationException("Unity 正在编译或导入资源，暂不能构建资源。");

                string platformDirectory = Path.GetFullPath(Path.Combine(NatAssetEditorUtil.GetEditorPath, target.ToString()));
                RejectReparsePoints(platformDirectory);
                string version = AllocateNextVersion(platformDirectory, DateTime.Now);
                string outputDirectory = Path.Combine(platformDirectory, version);
                // This attempt owns the new version directory until validation completes.
                ResourceBuildResult result;
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                    NatBuildConfig.Build(new NatBuildParamters(target, NatAssetBuildUtil.CollectBundles())
                    {
                        VersionData = VersionData.ConvertToVersionData(version),
                    });
                    result = InspectResourceVersion(outputDirectory, target);
                    NatAssetEditorFiles.WriteJson(Path.Combine(outputDirectory, "nat-asset-resource.json"), result);
                }
                catch
                {
                    NatAssetEditorFiles.TryDeleteFailedBuild(outputDirectory, platformDirectory);
                    throw;
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
                // Synchronization failure must not discard a successfully built resource version.
                if (syncEditorResources) NatAssetEditorResources.OnResourcesBuilt(result, owner);
                return result;
            }
        }

        public static ResourceBuildResult InspectResourceVersion(string directory, ValidBuildTarget target)
        {
            string root = NormalizeDirectory(directory);
            NatAssetManifest manifest = ReadAndValidate(root, target);
            return new ResourceBuildResult
            {
                Directory = root,
                Version = manifest.BuildVersion,
                Platform = manifest.Platform,
            };
        }

        /// <summary>Copies only the binary manifest and the files it names. The caller owns destination cleanup.</summary>
        public static void CopyResourceVersion(string sourceDirectory, string destination, ValidBuildTarget target)
        {
            string source = NormalizeDirectory(sourceDirectory);
            NatAssetManifest manifest = ReadAndValidate(source, target);
            string output = NormalizeDirectory(destination);
            if (PathsOverlap(source, output))
                throw new InvalidOperationException("资源源目录与目标目录不能相同或互相包含。");
            RejectReparsePoints(output);
            if (File.Exists(output) || (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()))
                throw new InvalidOperationException($"资源拷贝目标必须为空：{output}");

            Directory.CreateDirectory(output);
            foreach (BundleManifest bundle in manifest.BundleManifests)
            {
                string relative = ValidateRelativePath(bundle.RelativePath);
                string targetFile = ResolveFile(output, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.Copy(ResolveFile(source, relative), targetFile, false);
            }
            // The manifest is committed last so an interrupted copy never looks complete.
            File.Copy(ResolveFile(source, NatAssetEditorUtil.ConfigName),
                ResolveFile(output, NatAssetEditorUtil.ConfigName), false);
            ReadAndValidate(output, target);
        }

        public static string GetSettingsSummary()
        {
            return $"输出：{NatAssetEditorUtil.GetEditorPath}\n压缩：{NatAssetEditorUtil.BuildCompression}；" +
                   $"加密：{NatAssetEditorUtil.GloableEncrypt}；文件名附加 Hash：{NatAssetEditorUtil.AppendHash}";
        }

        public static void ClearBuildCache()
        {
#if NATASSET_SBP_SUPPORT
            UnityEditor.Build.Pipeline.Utilities.BuildCache.PurgeCache(true);
#else
            throw new InvalidOperationException("当前项目未启用 Scriptable Build Pipeline，无法清理资源构建缓存。");
#endif
        }

        internal static string AllocateNextVersion(string platformDirectory, DateTime now)
        {
            DateTime next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            if (Directory.Exists(platformDirectory))
            {
                foreach (string path in Directory.EnumerateFileSystemEntries(platformDirectory))
                {
                    if (!TryGetVersionTime(Path.GetFileName(path), out DateTime existing))
                        continue;
                    if (existing >= next)
                        next = existing.AddMinutes(1);
                }
            }
            return next.ToString("yyyy-MM-dd-HH-mm", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static NatAssetManifest ReadAndValidate(string root, ValidBuildTarget target)
        {
            ValidateTarget(target);
            RejectReparsePoints(root);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"资源版本目录不存在：{root}");
            string manifestPath = ResolveFile(root, NatAssetEditorUtil.ConfigName);
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("资源版本缺少 NatAsset 清单。", manifestPath);

            NatAssetManifest manifest;
            try
            {
                manifest = NatAssetManifest.DeserializeFromBinary(File.ReadAllBytes(manifestPath));
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is FormatException)
            {
                throw new InvalidDataException($"无法读取资源清单：{manifestPath}", exception);
            }
            if (manifest == null || manifest.NatAssetVersion != NatAssetSetting.NatAssetVersion)
                throw new InvalidDataException("NatAsset 清单格式版本与当前代码不匹配。");
            if (!string.Equals(manifest.Platform, target.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException($"资源平台 {manifest.Platform} 与目标 {target} 不一致。");
            if (!TryGetVersionTime(manifest.BuildVersion, out _) || !TryGetVersionTime(manifest.ReleaseVersion, out _))
                throw new InvalidDataException("资源清单包含无效版本号。");
            if (manifest.BundleManifests == null || manifest.BundleManifests.Count == 0)
                throw new InvalidDataException("资源清单没有任何 Bundle。");

            var bundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { NatAssetEditorUtil.ConfigName };
            foreach (BundleManifest bundle in manifest.BundleManifests)
            {
                if (bundle == null)
                    throw new InvalidDataException("资源清单包含空 Bundle。");
                if (target == ValidBuildTarget.WebGL && bundle.BundleEncrypt != BundleEncrypt.Nono)
                    throw new InvalidDataException($"WebGL 资源必须未加密，请重新打包资源：{bundle.BundlePath}");
                string bundlePath = ValidateRelativePath(bundle.BundlePath);
                if (!bundles.Add(bundlePath))
                    throw new InvalidDataException($"资源清单包含重复 Bundle：{bundle.BundlePath}");
                if (!IsHexHash(bundle.MD5) || !IsHexHash(bundle.Hash))
                    throw new InvalidDataException($"Bundle 的 MD5 或 Hash 无效：{bundle.BundlePath}");
                if (bundle.IsAppendHash && bundle.BundlePath.IndexOf('.') < 0)
                    throw new InvalidDataException($"附加 Hash 的 Bundle 路径缺少扩展名：{bundle.BundlePath}");
                string relative = ValidateRelativePath(bundle.RelativePath);
                if (!files.Add(relative))
                    throw new InvalidDataException($"资源清单包含重复文件：{relative}");
                string path = ResolveFile(root, relative);
                if (!File.Exists(path))
                    throw new FileNotFoundException("资源版本缺少 Bundle。", path);
                if ((ulong)new FileInfo(path).Length != bundle.Length)
                    throw new InvalidDataException($"Bundle 文件长度不匹配：{relative}");
                using (MD5 md5 = MD5.Create())
                using (FileStream stream = File.OpenRead(path))
                {
                    string actual = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                    if (!string.Equals(actual, bundle.MD5, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"Bundle MD5 不匹配：{relative}");
                }
            }
            foreach (BundleManifest bundle in manifest.BundleManifests)
            {
                if (bundle.Dependencies == null)
                    throw new InvalidDataException($"Bundle 依赖列表为空：{bundle.BundlePath}");
                foreach (string dependency in bundle.Dependencies)
                    if (!bundles.Contains(ValidateRelativePath(dependency)))
                        throw new InvalidDataException($"Bundle 依赖未包含在清单中：{dependency}");
            }
            return manifest;
        }

        private static bool TryGetVersionTime(string version, out DateTime value)
        {
            value = default;
            try
            {
                VersionData parsed = VersionData.ConvertToVersionData(version);
                value = new DateTime(parsed.Year, parsed.Month, parsed.Day, parsed.Hour, parsed.Minutes, 0);
                return true;
            }
            catch (Exception exception) when (exception is FormatException || exception is ArgumentException || exception is OverflowException)
            {
                return false;
            }
        }

        private static string NormalizeDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("资源目录不能为空。", nameof(directory));
            string fullPath = Path.GetFullPath(directory);
            if (string.Equals(fullPath, Path.GetPathRoot(fullPath), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("不能使用磁盘根目录作为资源版本目录。", nameof(directory));
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void ValidateTarget(ValidBuildTarget target)
        {
            if (!Enum.IsDefined(typeof(ValidBuildTarget), target))
                throw new ArgumentOutOfRangeException(nameof(target));
        }

        private static string ValidateRelativePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
                throw new InvalidDataException($"资源文件必须使用相对路径：{relative}");
            string normalized = relative.Replace('\\', '/');
            foreach (string segment in normalized.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == ".." ||
                    segment.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|' }) >= 0 ||
                    segment.Any(char.IsControl) || segment.EndsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith(" ", StringComparison.Ordinal))
                    throw new InvalidDataException($"资源清单包含不安全路径：{relative}");
            }
            return normalized;
        }

        private static string ResolveFile(string root, string relative)
        {
            string path = Path.GetFullPath(Path.Combine(root, ValidateRelativePath(relative)));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"资源路径越过版本目录：{relative}");
            RejectReparsePoints(path);
            return path;
        }

        private static void RejectReparsePoints(string path)
        {
            for (string current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException($"资源构建目录不能经过符号链接或目录联接：{current}");
            }
        }

        private static bool PathsOverlap(string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase) ||
                   first.StartsWith(second + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                   second.StartsWith(first + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHexHash(string value)
        {
            return value != null && value.Length == 32 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F'));
        }
    }
}
