using System.Collections.Generic;
using System.IO;

namespace NATFrameWork.NatAsset.Editor
{
    internal static class VersionConfig
    {
        internal static List<VersionInfo> GetVersionInfo(ValidBuildTarget buildTarget)
        {
            string path = Path.Combine(NatAssetEditorUtil.GetEditorPath, buildTarget.ToString());
            if (!Directory.Exists(path)) return null;
            List<VersionInfo> infos = new List<VersionInfo>();
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
            for(int i = 0;i < fileSystemInfos.Length; i++) 
            {
                FileSystemInfo fileSystemInfo = fileSystemInfos[i];
                if(fileSystemInfo is DirectoryInfo && fileSystemInfo.Name != NatAssetEditorUtil.SPECIAL_EditorFile)
                {
                    VersionInfo info = new VersionInfo();
                    info.Name = fileSystemInfo.Name;
                    info.FullName = fileSystemInfo.FullName;
                    infos.Add(info);
                }
            }
            return infos;
        } 
    }
}