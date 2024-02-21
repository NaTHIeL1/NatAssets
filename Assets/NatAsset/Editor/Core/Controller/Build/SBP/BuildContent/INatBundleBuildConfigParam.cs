#if NATASSET_SBP_SUPPORT
using NATFrameWork.NatAsset.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Pipeline.Interfaces;

namespace NATFrameWork.NatAsset.Editor
{
    public interface INatBundleBuildConfigParam:IContextObject
    {
        Dictionary<string, BundleBuildInfo> NatBundleInventoryBuilds { get; }
        BuildTarget TargetPlatform { get; }
        VersionData BuildVersion { get; }
    }
}
#endif