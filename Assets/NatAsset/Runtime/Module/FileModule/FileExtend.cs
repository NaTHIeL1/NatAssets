using System;
using System.IO;
using System.Security.Cryptography;
using Debug = UnityEngine.Debug;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class FileExtend
    {
        internal static string GetWebRequestPath(string path)
        {
            if (!path.Contains("://"))
            {
                path = "file://" + path;
            }

            return path;
        }

        internal static string NormalizePath(string originPath)
        {
            return originPath.Replace("\\", "/");
        }

        public static string GetFileMD5(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                MD5 tempMD5 = MD5.Create();
                byte[] fileMd5Bytes = tempMD5.ComputeHash(fs); // 计算FileStream 对象的哈希值  
                return BitConverter.ToString(fileMd5Bytes).Replace("-", "").ToLower();
            }
        }

        //public static string GetFileMD5(byte[] buffer,int offset, int count)
        //{

        //}
        
        internal static void CopyDicToNewPath(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            try
            {
                string[] labDirs = Directory.GetDirectories(sourcePath); //目录
                string[] labFiles = Directory.GetFiles(sourcePath); //文件
                if (labFiles.Length > 0)
                {
                    for (int i = 0; i < labFiles.Length; i++)
                    {
                        File.Copy(sourcePath + "\\" + Path.GetFileName(labFiles[i]),
                            targetPath + "\\" + Path.GetFileName(labFiles[i]), true);
                    }
                }

                if (labDirs.Length > 0)
                {
                    for (int j = 0; j < labDirs.Length; j++)
                    {
                        Directory.GetDirectories(sourcePath + "\\" + Path.GetFileName(labDirs[j]));

                        //递归调用
                        CopyDicToNewPath(sourcePath + "\\" + Path.GetFileName(labDirs[j]),
                            targetPath + "\\" + Path.GetFileName(labDirs[j]));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}