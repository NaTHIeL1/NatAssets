using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    internal enum CheckNewIn
    {
        ReadOnly,    //最新资源在只读区
        ReadWrite,   //最新资源在读写区
        Remote,      //最新资源在远端
        Dispose,     //CDN已不存在该资源包
    }

    /// <summary>
    /// 执行更新检测，比对只读，读写，远端区，得知最新资源版本，同时检测后决定是否要更新本地配置文件
    /// </summary>
    internal class CheckInfo
    {
        private readonly static string[] _defaultStr = new string[1];
        private string _checkName = string.Empty;
        private bool _needRemoveAsset = false;
        private bool _needUpdateManifest = false;

        /// <summary>
        /// 这个字段只表明更新的资源包在那个地方，不代表配置文件是否需要更新
        /// </summary>
        private CheckNewIn _checkNewIn = CheckNewIn.ReadOnly;

        internal BundleManifest ReadOnlyManifest;
        internal BundleManifest ReadWriteManifest;
        internal BundleManifest RemoteManifest;

        internal string CheckName { get { return _checkName; } }
        internal bool NeedRemoveAsset { get { return _needRemoveAsset; } }
        internal bool NeedUpdateManifest { get { return _needUpdateManifest; } }
        internal CheckNewIn CheckNewIn { get { return _checkNewIn; } }

        //internal string GetNewInGroup
        //{
        //    get
        //    {
        //        if (_needUpdateManifest)
        //            return RemoteManifest.MainGroup;
        //        switch (_checkNewIn)
        //        {
        //            case CheckNewIn.Nono:
        //                return string.Empty;
        //            case CheckNewIn.ReadOnly:
        //                return ReadOnlyManifest.MainGroup;
        //            case CheckNewIn.ReadWrite:
        //                return ReadWriteManifest.MainGroup;
        //            case CheckNewIn.Remote:
        //                return RemoteManifest.MainGroup;
        //        }
        //        return string.Empty;
        //    }
        //}

        internal string[] GetNewInDependence
        {
            get
            {
                if (_needUpdateManifest)
                    return RemoteManifest.Dependencies;
                switch (_checkNewIn)
                {
                    case CheckNewIn.Dispose:
                        return _defaultStr;
                    case CheckNewIn.ReadOnly:
                        return ReadOnlyManifest.Dependencies;
                    case CheckNewIn.ReadWrite:
                        return ReadWriteManifest.Dependencies;
                    case CheckNewIn.Remote:
                        return RemoteManifest.Dependencies;
                }
                return _defaultStr;
            }
        }

        public CheckInfo(string checknName)
        {
            _checkName = checknName;
        }

        /// <summary>
        /// 检测差异
        /// </summary>
        public void CheckUpadte()
        {
            if (RemoteManifest == null)
            {
                //如果读写区资源需要移除，那么相应的配置文件也需要修改
                _checkNewIn = CheckNewIn.Dispose;
                _needRemoveAsset = ReadWriteManifest != null;
                _needUpdateManifest = _needRemoveAsset;
                return;
            }
            if (ReadOnlyManifest != null && ReadOnlyManifest.EquipABVersion(RemoteManifest))
            {
                _checkNewIn = CheckNewIn.ReadOnly;
                _needRemoveAsset = ReadWriteManifest != null;
                _needUpdateManifest = ReadOnlyManifest.NeedUpdateManifest(RemoteManifest);
                //只要Manifset发生改变但资源版本未改变的情况下，需要下载一份资源到读写目录
                //确保bundle和manifest一一对应
                if (_needUpdateManifest)
                    _checkNewIn = CheckNewIn.Remote;
                return;
            }
            if (ReadWriteManifest != null && ReadWriteManifest.EquipABVersion(RemoteManifest))
            {
                _checkNewIn = CheckNewIn.ReadWrite;
                _needRemoveAsset = false;
                _needUpdateManifest = ReadOnlyManifest.NeedUpdateManifest(RemoteManifest);
                return;
            }

            _checkNewIn = CheckNewIn.Remote;
            _needRemoveAsset = ReadWriteManifest != null;
            _needUpdateManifest = true;
        }

        /// <summary>
        /// 是否存在差异
        /// </summary>
        /// <returns></returns>
        public bool HasDifference()
        {
            if (_checkNewIn == CheckNewIn.Remote || _needRemoveAsset || _needUpdateManifest)
                return true;
            return false;
        }

        public List<string> GetGroups()
        {
            switch (_checkNewIn)
            {
                case CheckNewIn.Dispose:
                    return null;
                case CheckNewIn.ReadOnly:
                    if (NeedUpdateManifest)
                        return RemoteManifest.Groups;
                    else
                        return ReadOnlyManifest.Groups;
                case CheckNewIn.ReadWrite:
                    if (NeedUpdateManifest)
                        return RemoteManifest.Groups;
                    else
                        return ReadWriteManifest.Groups;
                case CheckNewIn.Remote:
                    return RemoteManifest.Groups;
            }
            return null;
        }
    }
}
