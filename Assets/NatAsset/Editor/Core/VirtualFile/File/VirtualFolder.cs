using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    internal struct VirtualFolder : IVirtualFile
    {
        private Texture2D _fileObj;
        internal VirtualFolder(string fullName, string fileName, string name, IVirtualFile parent)
        {
            FullName = fullName;
            FileName = fileName;
            Name = name;
            IsDirectory = true;
            Parent = parent;
            Child = null;
            _fileObj = null;
        }

        public string Name { get; set; }
        public string FileName { get; set; }
        public string FullName { get; set; }
        public bool IsDirectory { get; set; }

        public Texture2D GetIcon
        {
            get
            {
                if (_fileObj == null)
                {
                    _fileObj = AssetDatabase.GetCachedIcon(FileName) as Texture2D;
                }

                return _fileObj;
            }
        }

        public IVirtualFile Parent { get; set; }
        public List<IVirtualFile> Child { get; set; }

        public void ForceBuild()
        {
            List<IVirtualFile> child = GetSubVirtualFiles();
            if (child == null) return;
            for (int i = 0; i < child.Count; i++)
            {
                child[i].ForceBuild();
            }

            Child = child;
        }

        public List<IVirtualFile> GetSubVirtualFiles()
        {
            List<IVirtualFile> results = null;
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(FileName));
            FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
            for (int i = 0; i < fileSystemInfos.Length; i++)
            {
                FileSystemInfo info = fileSystemInfos[i];
                if (Path.GetExtension(info.FullName) == ".meta")
                    continue;
                if (info is DirectoryInfo)
                {
                    //虚拟文件夹
                    string fileName = Path.Combine(FileName, info.Name).Replace(@"\", "/");
                    if (VirtualFileUtil.ContainsKey(fileName)) continue;
                    VirtualFolder virtualFolder = new VirtualFolder(info.FullName.Replace(@"\", "/"), fileName, info.Name, this);
                    if (results == null)
                        results = new List<IVirtualFile>();
                    results.Add(virtualFolder);
                }
                else
                {
                    //虚拟资源文件
                    string fileName = Path.Combine(FileName, info.Name).Replace(@"\", "/");
                    if (VirtualFileUtil.ContainsKey(fileName)) continue;
                    VirtualFile virtualFile = new VirtualFile(info.FullName.Replace(@"\", "/"), fileName, info.Name, this);
                    if (results == null)
                        results = new List<IVirtualFile>();
                    results.Add(virtualFile);
                }
            }

            return results;
        }
    }
}