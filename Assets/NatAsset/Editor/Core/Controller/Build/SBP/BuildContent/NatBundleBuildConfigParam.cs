using NATFrameWork.NatAsset.Runtime;
#if NATASSET_SBP_SUPPORT
using System.Collections.Generic;
using UnityEditor;

namespace NATFrameWork.NatAsset.Editor
{
    public class NatBundleBuildConfigParam:INatBundleBuildConfigParam
    {
        public Dictionary<string, BundleBuildInfo> NatBundleInventoryBuilds { get; }
        public BuildTarget TargetPlatform { get; }

        public VersionData BuildVersion { get; }

        public NatBundleBuildConfigParam(Dictionary<string, BundleBuildInfo> bundleInventoryBuilds, BuildTarget buildTarget, VersionData buildVersion)
        {
            NatBundleInventoryBuilds = bundleInventoryBuilds;
            TargetPlatform = buildTarget;
            BuildVersion = buildVersion;
        }
    }
}
#endif