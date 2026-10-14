using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    /// <summary>One owner per source file, shared by all typed load results.</summary>
    internal sealed class AssetLifetimeInfo
    {
        internal readonly string Path;
        internal readonly long Generation;
        internal readonly List<AssetInfo> Assets = new List<AssetInfo>();
        internal readonly HashSet<AssetLifetimeInfo> Dependencies = new HashSet<AssetLifetimeInfo>();
        internal readonly HashSet<AssetLifetimeInfo> ResidentDependents = new HashSet<AssetLifetimeInfo>();
        internal AssetManifest Manifest;
        internal string[] Bundles;
        internal bool EditorAsset;
        internal bool Resident;
        internal bool GraphReady;
        internal bool Active;
        internal int ExternalRefCount;
        internal int SceneRefCount;
        internal int DependencyRefCount;
        internal int PendingLoadCount;
        internal BaseTask UnloadTask;

        internal AssetLifetimeInfo(string path, long generation)
        {
            Path = path;
            Generation = generation;
        }

        internal bool Locked
        {
            get
            {
                foreach (AssetInfo asset in Assets) if (asset.isLocked) return true;
                return false;
            }
        }
    }
}
