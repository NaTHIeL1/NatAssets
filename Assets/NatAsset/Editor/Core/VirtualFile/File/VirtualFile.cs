using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace NATFrameWork.NatAsset.Editor
{
    [Serializable]
    internal struct VirtualFile:IVirtualFile
    {
        private Texture2D _fileObj;
        internal VirtualFile(string fullName, string fileName, string name, IVirtualFile parent)
        {
            FullName = fullName;
            FileName = fileName;
            Name = name;
            IsDirectory = false;
            Parent = parent;
            Child = null;
            _fileObj = null;
        }

        public string Name { get; set; }
        public string FileName { get; set; }
        public string FullName { get; set; }
        public bool IsDirectory { get; set; }
        public IVirtualFile Parent { get; set; }
        public List<IVirtualFile> Child { get; set; }

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

        public void ForceBuild()
        {
            _fileObj = null;
        }

        public List<IVirtualFile> GetSubVirtualFiles()
        {
            return null;
        }
    }
}