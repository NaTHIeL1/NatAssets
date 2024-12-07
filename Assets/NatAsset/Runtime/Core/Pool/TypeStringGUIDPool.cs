using System;
using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class TypeStringGUIDPool
    {
        private static Dictionary<Type, Dictionary<string, string>> _typeStringDic 
            = new Dictionary<Type, Dictionary<string, string>>();
        internal static void Release()
        {
            _typeStringDic?.Clear();
        }

        internal static string GetTargetGUID(Type type, string sourceName)
        {
            Dictionary<string, string> targetDic;
            if (!_typeStringDic.TryGetValue(type, out targetDic))
            {
                targetDic = new Dictionary<string, string>();
                _typeStringDic.Add(type, targetDic);
            }

            string targetGUID = string.Empty;
            if(!targetDic.TryGetValue(sourceName, out targetGUID))
            {
                targetGUID = $"{sourceName}-{type.FullName}";
                targetDic.Add(sourceName, targetGUID);
            }

            return targetGUID;
        }

        internal static string GetTargetGUID(AssetInfo assetInfo)
        {
            Dictionary<string, string> targetDic;
            Type type = assetInfo.AssetType;
            string name = assetInfo.InfoNameGUID;
            if (!_typeStringDic.TryGetValue(type, out targetDic))
            {
                targetDic = new Dictionary<string, string>();
                _typeStringDic.Add(type, targetDic);
            }

            string targetGUID = string.Empty;
            if (!targetDic.TryGetValue(name, out targetGUID))
            {
                targetGUID = $"{name}-{type.FullName}";
                targetDic.Add(name, targetGUID);
            }

            return targetGUID;
        }
    }
}
