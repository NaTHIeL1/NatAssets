using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NATFrameWork.NatAsset.Editor
{
    internal static class VirtualFileUtil
    {
        internal static List<PackageVirtualRoot> VirtualFiles
        {
            get { return NatAssetBuildSetting.Instance.VirtualFiles; }
            set { NatAssetBuildSetting.Instance.VirtualFiles = value; }
        }

        internal static void RefreshVirtualRoot()
        {
            //string path = Path.Combine("Assets", NatAssetEditorUtil.ResPath);
            //DirectoryInfo directoryInfo = new DirectoryInfo(path);
            //FileSystemInfo[] fileSystemInfos = directoryInfo.GetFileSystemInfos();
            //for (int i = 0; i < fileSystemInfos.Length; i++)
            //{
            //    FileSystemInfo info = fileSystemInfos[i];
            //    if (Path.GetExtension(info.FullName) == ".meta") continue;
            //    if (info is DirectoryInfo)
            //    {
            //        string fullName = Path.Combine(path, info.Name);
            //        AddPackageVirtualRoot(fullName);
            //    }
            //    else
            //    {
            //        string fullName = Path.Combine(path, info.Name);
            //        AddPackageVirtualRoot(fullName);
            //    }
            //}
        }

        internal static void AddPackageVirtualRoot(string filePath)
        {
            List<PackageVirtualRoot> virtualRoots = VirtualFiles;
            if (virtualRoots == null)
                virtualRoots = VirtualFiles = new List<PackageVirtualRoot>();
            for (int i = 0; i < virtualRoots.Count; i++)
            {
                if (virtualRoots[i].FileName == filePath)
                {
                    return;
                }
            }


            if (File.Exists(filePath))
            {
                if (Path.GetExtension(filePath) == ".meta")
                {
                    Debug.LogWarning($".meta文件不能打入bundle");
                    return;
                }

                string fileName = Path.GetFileName(filePath);
                string targetPath = filePath.Replace(@"\", "/");
                virtualRoots.Add(new PackageVirtualRoot(targetPath, fileName, false, null));
            }
            else if (Directory.Exists(filePath))
            {
                string directoryName = Path.GetFileName(filePath);
                string targetPath = filePath.Replace(@"\", "/");
                virtualRoots.Add(new PackageVirtualRoot(targetPath, directoryName, true, null));
            }
            else
            {
                Debug.LogError($"{filePath}既非目录也非文件");
            }
        }

        internal static void RemovePackageVirtualRoot(string filePath)
        {
            List<PackageVirtualRoot> virtualRoots = VirtualFiles;
            if (virtualRoots == null) return;
            for (int i = 0; i < virtualRoots.Count; i++)
            {
                if (virtualRoots[i].FileName == filePath)
                {
                    virtualRoots.RemoveAt(i);
                    return;
                }
            }
        }

        internal static bool ContainsKey(string filePath)
        {
            List<PackageVirtualRoot> virtualRoots = VirtualFiles;
            if (virtualRoots == null) return false;
            for (int i = 0; i < virtualRoots.Count; i++)
            {
                if (virtualRoots[i].FileName == filePath)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasSubFolder(this IVirtualFile virtualFile)
        {
            if (virtualFile.Child == null) return false;
            for (int i = 0; i < virtualFile.Child.Count; i++)
            {
                IVirtualFile temp = virtualFile.Child[i];
                if (temp.IsDirectory)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void Sort()
        {
            List<PackageVirtualRoot> virtualRoots = VirtualFiles;
            if (virtualRoots == null) return;
            IOrderedEnumerable<PackageVirtualRoot> ordered = virtualRoots.Order(info => info.FileName, true);
            VirtualFiles = new List<PackageVirtualRoot>(ordered);
        }


        #region 构建虚拟目录

        /// <summary>
        /// 通过缓存的Root节点构建出临时的虚拟目录
        /// </summary>
        internal static void BuildVirtualFile()
        {
            if (VirtualFiles == null || VirtualFiles.Count == 0)
                return;
            for (int i = 0; i < VirtualFiles.Count; i++)
            {
                IVirtualRoot virtualRoot = VirtualFiles[i];
                virtualRoot.ForceBuild();
            }
        }

        /// <summary>
        /// 通过构建出的虚拟目录，构建TreeViewItem树
        /// </summary>
        internal static void BuildVirtualFileTreeView(TreeViewItem<IVirtualFile> root)
        {
            if (VirtualFiles == null || VirtualFiles.Count == 0)
                return;
            int index = 0;
            for (int i = 0; i < VirtualFiles.Count; i++)
            {
                IVirtualFile virtualFile = VirtualFiles[i];
                TreeViewItem<IVirtualFile> viewItem =
                    new TreeViewItem<IVirtualFile>(++index, 0, virtualFile.Name, virtualFile);
                root.AddChild(viewItem);
                BuildChildVirtualFileTreeView(viewItem, 1, ref index);
            }
        }

        private static void BuildChildVirtualFileTreeView(TreeViewItem<IVirtualFile> root, int depth, ref int index)
        {
            List<IVirtualFile> virtualFiles = root.data.Child;
            if (virtualFiles == null || virtualFiles.Count == 0)
                return;
            for (int i = 0; i < virtualFiles.Count; i++)
            {
                IVirtualFile virtualFile = virtualFiles[i];
                TreeViewItem<IVirtualFile> viewItem =
                    new TreeViewItem<IVirtualFile>(++index, depth, virtualFile.Name, virtualFile);
                root.AddChild(viewItem);
                BuildChildVirtualFileTreeView(viewItem, depth + 1, ref index);
            }
        }

        #endregion
    }
}