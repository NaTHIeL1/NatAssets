using NATFrameWork.NatAsset.Runtime;

namespace NATFrameWork.NatAsset.Editor
{
    interface IVirtualRoot:IVirtualFile
    {
        string Group { get; set; }
        string CollectorName { get; set; }
        EditorBundleEncrypt EncryptName { get; set; }
    }
}