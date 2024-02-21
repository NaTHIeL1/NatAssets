using System;
using System.Collections.Generic;
using System.IO;
using NATFrameWork.NatAsset.Runtime;
using UnityEditor;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    public class PackageVirtualRoot : IVirtualRoot
    {
        private Texture2D _fileObj;
        [SerializeField] private string _name;
        [SerializeField] private string _fileName;
        private string _fullNames = String.Empty;
        [SerializeField] private bool _isDirectory;
        [SerializeField] private string _collectorName;
        [SerializeField] private EditorBundleEncrypt _encryptName;
        [SerializeField] private string _group = "Base";

        internal PackageVirtualRoot(string fileName, string name, bool isDirectory,
            IVirtualFile parent)
        {
            FileName = fileName;
            Name = name;
            IsDirectory = isDirectory;
            Parent = parent;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        [HideInInspector]
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(_fullNames))
                {
                    _fullNames = Path.GetFullPath(FileName).Replace(@"\", "/");
                }
                return _fullNames;
            }
            set { _fullNames = value; }
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set => _isDirectory = value;
        }

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
        [HideInInspector] public List<IVirtualFile> Child { get; set; }

        public string Group
        {
            get => _group;
            set => _group = value;
        }

        public string CollectorName
        {
            get
            {
                if (string.IsNullOrEmpty(_collectorName))
                    _collectorName = typeof(NAssetToOneBundle).Name;
                return _collectorName;
            }
            set => _collectorName = value;
        }

        public EditorBundleEncrypt EncryptName
        {
            get => _encryptName;
            set => _encryptName = value;
        }

        public void ForceBuild()
        {
            Child = null;
            if (IsDirectory)
            {
                List<IVirtualFile> child = GetSubVirtualFiles();
                if (child == null) return;
                for (int i = 0; i < child.Count; i++)
                {
                    child[i].ForceBuild();
                }

                Child = child;
            }
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
                    VirtualFolder virtualFolder =
                        new VirtualFolder(info.FullName.Replace(@"\", "/"), fileName, info.Name, this);
                    if (results == null)
                        results = new List<IVirtualFile>();
                    results.Add(virtualFolder);
                }
                else
                {
                    //虚拟资源文件
                    string fileName = Path.Combine(FileName, info.Name).Replace(@"\", "/");
                    if (VirtualFileUtil.ContainsKey(fileName)) continue;
                    VirtualFile virtualFile =
                        new VirtualFile(info.FullName.Replace(@"\", "/"), fileName, info.Name, this);
                    if (results == null)
                        results = new List<IVirtualFile>();
                    results.Add(virtualFile);
                }
            }

            return results;
        }
    }
}