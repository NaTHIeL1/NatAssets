namespace NATFrameWork.NatAsset.Editor
{
    public interface OnBeforeBuildExe
    {
        public int Priority { get; }
        public bool ExecutBeforBuild();
    }
}