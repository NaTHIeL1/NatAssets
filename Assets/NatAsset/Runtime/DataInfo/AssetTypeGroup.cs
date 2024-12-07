//using System.Collections.Generic;

//namespace NATFrameWork.NatAsset.Runtime
//{
//    internal class AssetTypeGroup
//    {
//        private Dictionary<string, AssetInfo> _assetGroupDic = null;

//        public AssetTypeGroup() 
//        {
//            _assetGroupDic = new Dictionary<string, AssetInfo>();
//        }

//        internal AssetInfo GetAssetInfo(string assetPath)
//        {
//            if(_assetGroupDic.TryGetValue(assetPath, out AssetInfo assetInfo))
//            {
//                return assetInfo;
//            }
//            return null;
//        }

//        internal void AddAssetInfo(AssetInfo assetInfo)
//        {
//            if(!_assetGroupDic.ContainsKey(assetInfo.InfoNameGUID))
//            {
//                _assetGroupDic.Add(assetInfo.InfoNameGUID, assetInfo);
//            }
//        }

//        internal void RemoveAssetInfo(AssetInfo assetInfo) 
//        {
//            if(_assetGroupDic.ContainsKey(assetInfo.InfoNameGUID))
//            {
//                _assetGroupDic.Remove(assetInfo.InfoNameGUID);
//            }
//        }

//        internal void CollectionMark()
//        {
//            foreach(var kv in _assetGroupDic)
//            {
//                kv.Value.CollectionMark();
//            }
//        }

//        internal void RedRefCount()
//        {
//            foreach (var kv in _assetGroupDic)
//            {
//                kv.Value.RedRefCount();
//            }
//        }
//    }
//}
