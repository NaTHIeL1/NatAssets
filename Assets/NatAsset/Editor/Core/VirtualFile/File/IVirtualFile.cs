using System;
using System.Collections.Generic;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    public interface IVirtualFile
    {
        string Name { get; set; }
        string FileName { get; set; }
        string FullName { get; set; }
        bool IsDirectory { get; set; }
        Texture2D GetIcon { get; }
        IVirtualFile Parent { get; set; }
        List<IVirtualFile> Child { get; set; }
        void ForceBuild();
        List<IVirtualFile> GetSubVirtualFiles();
    }
}