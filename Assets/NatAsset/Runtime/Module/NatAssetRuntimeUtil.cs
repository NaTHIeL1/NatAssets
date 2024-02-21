using System.IO;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class NatAssetRuntimeUtil
    {
        public static bool CheckWebRequestError(UnityWebRequest request)
        {
#if UNITY_2020_3_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        public static bool CheckReadWriteBundle(string path, BundleManifest bundleManifest, bool onlyLength = false)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            FileInfo fileInfo = new FileInfo(path);
            bool isMatchLength = (ulong)fileInfo.Length == bundleManifest.Length;
            if (isMatchLength && !onlyLength)
            {
                string md5 = FileExtend.GetFileMD5(path);
                return md5 == bundleManifest.MD5;
            }
            return isMatchLength;
        }
    }
}
