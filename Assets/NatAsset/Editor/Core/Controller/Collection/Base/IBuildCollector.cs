using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Editor
{
    public interface IBuildCollector
    {
        List<BundleBuildInfo> OnCollection(PackageVirtualRoot packageRoot);
    }
}