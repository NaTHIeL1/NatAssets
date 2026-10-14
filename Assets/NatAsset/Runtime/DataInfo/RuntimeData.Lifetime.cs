using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static partial class RuntimeData
    {
        private static readonly Dictionary<string, AssetLifetimeInfo> _lifetimes =
            new Dictionary<string, AssetLifetimeInfo>(StringComparer.Ordinal);
        private static long _lifetimeGeneration;
        private static long _unloadSequence;
        private static bool _lifetimeDirty;
        internal static bool HasLifetimeChanges => _lifetimeDirty;
        internal static bool IsReleasing { get; private set; }

        internal static bool IsEditorAssetMode
        {
            get
            {
#if UNITY_EDITOR
                return NatAssetSetting.TRunWay == RunWay.Editor;
#else
                return false;
#endif
            }
        }

        internal static string NormalizeAssetPath(string path) => path?.Replace('\\', '/') ?? string.Empty;

        internal static AssetLifetimeInfo GetLifetime(string path) =>
            _lifetimes.TryGetValue(NormalizeAssetPath(path), out AssetLifetimeInfo value) ? value : null;

        private static AssetLifetimeInfo GetOrCreateLifetime(string path, bool editor)
        {
            path = NormalizeAssetPath(path);
            if (_lifetimes.TryGetValue(path, out AssetLifetimeInfo result)) return result;
            AssetManifest manifest = editor ? null : GetAssetManifest(path);
            if (!editor && manifest == null) throw new InvalidDataException("资源未登记到清单: " + path);
            result = new AssetLifetimeInfo(path, ++_lifetimeGeneration)
            {
                EditorAsset = editor,
                Manifest = manifest,
                Bundles = manifest == null ? Array.Empty<string>() : GetLifetimeBundles(manifest.BundleName)
            };
            _lifetimes.Add(path, result);
            return result;
        }

        private static string[] GetLifetimeBundles(string owner)
        {
            var bundles = new HashSet<string>(GetAllDependencies(owner), StringComparer.Ordinal) { owner };
            bundles.Remove(string.Empty);
            var result = new string[bundles.Count];
            bundles.CopyTo(result);
            return result;
        }

        private static AssetLifetimeInfo PrepareLifetime(string path, bool editor)
        {
            AssetLifetimeInfo root = GetOrCreateLifetime(path, editor);
            if (root.GraphReady) return root;
            // Each active root retains its reachable dependencies once. This handles diamonds and
            // cycles without self-sustaining reference counts or recursive dependency load tasks.
            var visited = new HashSet<string>(StringComparer.Ordinal) { root.Path };
            var pending = new Stack<string>();
            if (root.Manifest != null)
                foreach (string dependency in root.Manifest.Dependencies) pending.Push(dependency);
            while (pending.Count > 0)
            {
                string dependency = NormalizeAssetPath(pending.Pop());
                if (!visited.Add(dependency)) continue;
                AssetLifetimeInfo node = GetOrCreateLifetime(dependency, editor);
                root.Dependencies.Add(node);
                if (node.Manifest != null)
                    foreach (string child in node.Manifest.Dependencies) pending.Push(child);
            }
            root.GraphReady = true;
            return root;
        }

        internal static AssetLifetimeInfo AcquireAssetLoad(string path, bool editor)
        {
            if (IsReleasing) throw new InvalidOperationException("NatAsset 正在退出，不能加载新资源。");
            AssetLifetimeInfo root = PrepareLifetime(path, editor);
            ChangePending(root, 1);
            return root;
        }

        internal static void ReleaseAssetLoad(AssetLifetimeInfo root)
        {
            if (root != null && ReferenceEquals(GetLifetime(root.Path), root)) ChangePending(root, -1);
        }

        private static void ChangePending(AssetLifetimeInfo root, int delta)
        {
            root.PendingLoadCount += delta;
            if (root.PendingLoadCount < 0) throw new InvalidOperationException("加载保留重复归还: " + root.Path);
            CancelAssetUnload(root);
            foreach (AssetLifetimeInfo dependency in root.Dependencies)
            {
                dependency.PendingLoadCount += delta;
                if (dependency.PendingLoadCount < 0) throw new InvalidOperationException("依赖保留重复归还: " + dependency.Path);
                CancelAssetUnload(dependency);
            }
            InvalidateBundleCandidates();
        }

        internal static void RegisterAssetLifetime(AssetInfo asset)
        {
            AssetLifetimeInfo root = PrepareLifetime(asset.InfoNameGUID, IsEditorAssetMode);
            asset.Lifetime = root;
            root.Assets.Add(asset);
            MarkResident(root);
            MarkLifetimeDirty();
        }

        private static void MarkResident(AssetLifetimeInfo root)
        {
            if (root.Resident) return;
            root.Resident = true;
            foreach (AssetLifetimeInfo dependency in root.Dependencies)
            {
                dependency.ResidentDependents.Add(root);
                CancelAssetUnload(dependency);
            }
        }

        internal static AssetLifetimeInfo RegisterSceneLifetime(string path)
        {
            AssetLifetimeInfo root = PrepareLifetime(path, false);
            root.SceneRefCount++;
            MarkResident(root);
            RefreshAssetReferences(root);
            return root;
        }

        internal static void ReleaseSceneLifetime(AssetLifetimeInfo root)
        {
            if (root == null || IsReleasing) return;
            if (--root.SceneRefCount < 0) throw new InvalidOperationException("场景引用重复归还: " + root.Path);
            RefreshAssetReferences(root);
            if (root.SceneRefCount == 0) ClearResidentAsset(root);
        }

        internal static void RefreshAssetReferences(AssetLifetimeInfo root)
        {
            if (root == null) return;
            int references = root.SceneRefCount;
            foreach (AssetInfo asset in root.Assets) references += asset.RefCount;
            root.ExternalRefCount = references;
            bool active = references > 0 || root.Locked;
            if (active != root.Active)
            {
                root.Active = active;
                foreach (AssetLifetimeInfo dependency in root.Dependencies)
                {
                    dependency.DependencyRefCount += active ? 1 : -1;
                    if (dependency.DependencyRefCount < 0)
                        throw new InvalidOperationException("依赖引用重复归还: " + dependency.Path);
                    CancelAssetUnload(dependency);
                }
                InvalidateBundleCandidates();
            }
            CancelAssetUnload(root);
            MarkLifetimeDirty();
        }

        internal static void MarkLifetimeDirty() => _lifetimeDirty = true;

        private static void InvalidateBundleCandidates()
        {
            foreach (BundleInfo bundle in _bundleDic.Values)
                if (GetUnloadBundleGroup(bundle, out _) == null) bundle.CancelUnload();
            MarkLifetimeDirty();
        }

        private static void CancelAssetUnload(AssetLifetimeInfo root)
        {
            root.UnloadTask?.CancelTask();
            root.UnloadTask = null;
        }

        internal static string AssetUnloadBlocker(AssetLifetimeInfo root)
        {
            if (root.ExternalRefCount != 0 || root.DependencyRefCount != 0) return "Referenced";
            if (root.PendingLoadCount != 0) return "Loading";
            if (root.Locked) return "Locked";
            if (root.Assets.Count == 0) return "NoLoadedObject";
            if (root.EditorAsset) return null; // Editor cache eviction never destroys Unity objects.
            if (root.Manifest.IsAtlasPackable) return "AtlasMember";
            if (root.Manifest.UnloadPolicy != AssetUnloadPolicy.Independent) return "BundleOnly";
            if (root.ResidentDependents.Count != 0) return "ResidentDependent";
            foreach (AssetInfo asset in root.Assets)
                if (!(asset.Asset is TextAsset) && !(asset.Asset is Texture2D)) return "BundleOnly";
            return null;
        }

        internal static void ScheduleUnusedLifetimes()
        {
            if (!_lifetimeDirty || IsReleasing) return;
            _lifetimeDirty = false;
            foreach (AssetLifetimeInfo root in _lifetimes.Values)
            {
                if (root.UnloadTask != null || AssetUnloadBlocker(root) != null) continue;
                var parameter = new ComTaskParam("asset-unload:" + ++_unloadSequence, root.Path);
                root.UnloadTask = BaseTask.CreateTask<AssetUnLoadTask>(parameter, Priority.Middle);
                TaskSystem.UnLoadTaskRunner.AddTask(root.UnloadTask);
            }
            foreach (BundleInfo bundle in _bundleDic.Values)
                if (GetUnloadBundleGroup(bundle, out _) != null) bundle.ScheduleUnload();
        }

        internal static bool TryUnloadAsset(AssetLifetimeInfo root, long generation, BaseTask task)
        {
            if (root == null || root.Generation != generation || !ReferenceEquals(GetLifetime(root.Path), root)
                || !ReferenceEquals(root.UnloadTask, task)) return false;
            root.UnloadTask = null;
            if (AssetUnloadBlocker(root) != null) return false;
            if (!root.EditorAsset)
            {
                var objects = new HashSet<Object>();
                foreach (AssetInfo asset in root.Assets)
                    if (asset.Asset is Object obj && obj != null) objects.Add(obj);
                foreach (Object obj in objects) Resources.UnloadAsset(obj);
            }
            ClearResidentAsset(root);
            return true;
        }

        private static void ClearResidentAsset(AssetLifetimeInfo root)
        {
            CancelAssetUnload(root);
            foreach (AssetInfo asset in root.Assets)
            {
                RemoveAssetInfo(asset);
                asset.Lifetime = null;
                BaseInfo.Release(asset);
            }
            root.Assets.Clear();
            root.Resident = false;
            foreach (AssetLifetimeInfo dependency in root.Dependencies) dependency.ResidentDependents.Remove(root);
            // Keep small graph nodes for the manifest lifetime; never retain discarded Unity objects.
            if (root.EditorAsset && root.PendingLoadCount == 0) _lifetimes.Remove(root.Path);
            MarkLifetimeDirty();
        }

        internal static List<BundleInfo> GetUnloadBundleGroup(BundleInfo target, out string blocker)
        {
            blocker = null;
            if (target == null || !ReferenceEquals(GetBundle(target.InfoNameGUID), target))
            { blocker = "StaleBundle"; return null; }
            var group = new List<BundleInfo> { target };
            var paths = new HashSet<string>(StringComparer.Ordinal) { target.InfoNameGUID };
            var targetDependencies = new HashSet<string>(GetAllDependencies(target.InfoNameGUID), StringComparer.Ordinal);
            foreach (BundleInfo candidate in _bundleDic.Values)
                if (!ReferenceEquals(candidate, target) && targetDependencies.Contains(candidate.InfoNameGUID)
                    && Array.IndexOf(GetAllDependencies(candidate.InfoNameGUID), target.InfoNameGUID) >= 0)
                { group.Add(candidate); paths.Add(candidate.InfoNameGUID); }
            foreach (BundleInfo member in group)
                if (member.RefCount != 0 || member.isLocked)
                { blocker = "ReferencedOrLoading"; return null; }
            foreach (AssetLifetimeInfo root in _lifetimes.Values)
            {
                if (root.ExternalRefCount == 0 && root.DependencyRefCount == 0 && root.PendingLoadCount == 0 && !root.Locked) continue;
                foreach (string bundle in root.Bundles)
                    if (paths.Contains(bundle)) { blocker = "AssetInUse:" + root.Path; return null; }
            }
            // A loaded dependent may still contain zero-ref prefabs referencing this bundle.
            foreach (BundleInfo other in _bundleDic.Values)
            {
                if (paths.Contains(other.InfoNameGUID)) continue;
                foreach (string dependency in GetAllDependencies(other.InfoNameGUID))
                    if (paths.Contains(dependency)) { blocker = "ResidentBundle:" + other.InfoNameGUID; return null; }
            }
            return group;
        }

        internal static bool TryUnloadBundle(BundleInfo target, long generation, BaseTask task)
        {
            if (target == null || target.Generation != generation || !target.IsUnloadTask(task)) return false;
            target.FinishUnloadTask(task);
            List<BundleInfo> group = GetUnloadBundleGroup(target, out _);
            if (group == null) return false;
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (BundleInfo member in group) paths.Add(member.InfoNameGUID);
            foreach (BundleInfo member in group)
            {
                member.CancelUnload();
                if (member.Bundle != null) member.Bundle.Unload(true);
                RemoveBundleInfo(member);
            }
            foreach (AssetLifetimeInfo root in _lifetimes.Values)
                if (root.Manifest != null && paths.Contains(root.Manifest.BundleName)) ClearResidentAsset(root);
            foreach (BundleInfo member in group) BaseInfo.Release(member);
            MarkLifetimeDirty();
            return true;
        }

        internal static void BeginRelease() => IsReleasing = true;

#if UNITY_EDITOR
        internal static List<string> GetLifetimeDiagnostics()
        {
            var result = new List<string>();
            foreach (AssetLifetimeInfo root in _lifetimes.Values)
            {
                var dependents = new List<string>();
                foreach (AssetLifetimeInfo dependent in root.ResidentDependents) dependents.Add(dependent.Path);
                result.Add($"{root.Path} | External={root.ExternalRefCount}, Dependencies={root.DependencyRefCount}, " +
                    $"Loading={root.PendingLoadCount}, Locked={root.Locked}, Resident={root.Resident}, " +
                    $"Generation={root.Generation}, Editor={root.EditorAsset}, Blocker={AssetUnloadBlocker(root) ?? "None"}, " +
                    $"Bundles=[{string.Join(", ", root.Bundles)}], ResidentDependents=[{string.Join(", ", dependents)}]");
            }
            return result;
        }
#endif

        internal static async System.Threading.Tasks.Task UnloadManagedScenesAsync()
        {
            foreach (SceneInfo scene in new List<SceneInfo>(_sceneDic.Values))
            {
                if (scene.Lifetime == null || !scene.Scene.IsValid() || !scene.Scene.isLoaded) continue;
                if (UnityEngine.SceneManagement.SceneManager.sceneCount == 1)
                    UnityEngine.SceneManagement.SceneManager.CreateScene("NatAssetReleaseScene");
                var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene.Scene);
                if (operation != null)
                    while (!operation.isDone) await System.Threading.Tasks.Task.Yield();
            }
        }

        internal static void ReleaseOwnedBundles()
        {
            foreach (BundleInfo bundle in _bundleDic.Values)
            {
                if (bundle.Bundle != null) bundle.Bundle.Unload(true);
                BaseInfo.Release(bundle);
            }
        }

        internal static void ClearLifetimes()
        {
            foreach (AssetLifetimeInfo root in _lifetimes.Values)
            {
                root.UnloadTask = null;
                foreach (AssetInfo asset in root.Assets) { asset.Lifetime = null; BaseInfo.Release(asset); }
                root.Assets.Clear();
                root.Dependencies.Clear();
                root.ResidentDependents.Clear();
            }
            _lifetimes.Clear();
            _lifetimeDirty = false;
        }
    }
}
