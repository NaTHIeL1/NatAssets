using System;
using System.IO;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class EncryptionUtil
    {
        public const int EncryptOffset = 32;
        public const byte EncryptOffsetHead = 32;

        /// <summary>
        /// 偏移加密
        /// </summary>
        /// <param name="filePath"></param>
        public static void EncryptByOffSet(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            int fileLength = EncryptOffset + bytes.Length;
            byte[] resultBytes = new byte[fileLength];
            for (int i = 0; i < EncryptOffset; i++)
            {
                resultBytes[i] = EncryptOffsetHead;
            }

            Array.Copy(bytes, 0, resultBytes, EncryptOffset, bytes.Length);
            using (FileStream fs = File.OpenWrite(filePath))
            {
                fs.Position = 0;
                fs.Write(resultBytes, 0, fileLength);
            }
        }


    }

    public enum BundleEncrypt:byte
    {
        Nono,
        Offset,
    }

    public enum EditorBundleEncrypt : byte
    {
        Global,
        Nono,
        Offset,
    }
    
    public enum GloableEncrypt : byte
    {
        Nono,
        Offset,
    }
}