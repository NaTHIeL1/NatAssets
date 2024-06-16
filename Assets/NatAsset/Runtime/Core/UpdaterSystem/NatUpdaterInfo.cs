using System.Collections.Generic;

namespace NATFrameWork.NatAsset.Runtime
{
    public class NatUpdaterInfo
    {
        private readonly static ulong manifestLength = 1;
        /// <summary>
        /// 是否检查成功
        /// </summary>
        public bool Success { get; internal set; }

        /// <summary>
        /// 检查失败时的异常信息
        /// </summary>
        public string Error { get; internal set; }

        /// <summary>
        /// 需要更新的资源包总数量
        /// </summary>
        public int TotalCount { get; internal set; }

        /// <summary>
        /// 需要更新的资源包总长度
        /// </summary>
        public ulong TotalLength { get; internal set; }

        public NatUpdaterInfo() { }

        public NatUpdaterInfo(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        //有差异的资源列表
        private Dictionary<string, CheckInfo> _cacheCheckInfoDic = new Dictionary<string, CheckInfo>();

        internal Dictionary<string, CheckInfo> CheckInfoDic
        {
            get { return _cacheCheckInfoDic; }
        }

        /// <summary>
        /// 添加需要下载的最新的BundleManifest信息
        /// </summary>
        /// <param name="bundleManifest">来自cdn的manifest中</param>
        internal void AddCheckInfo(CheckInfo checkInfo)
        {
            if (_cacheCheckInfoDic == null)
                _cacheCheckInfoDic = new Dictionary<string, CheckInfo>();
            if (!_cacheCheckInfoDic.ContainsKey(checkInfo.CheckName))
            {
                _cacheCheckInfoDic.Add(checkInfo.CheckName, checkInfo);
            }
        }

        /// <summary>
        /// 检查单个Group是否有差异，哪怕是配置文件有差异，也算有更新
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool CheckGroupHasUpdate(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;

            foreach (var kv in _cacheCheckInfoDic)
            {
                CheckInfo checkInfo = kv.Value;
                List<string> groups = checkInfo.GetGroups();
                if (checkInfo.CheckNewIn == CheckNewIn.Dispose)
                    return true;

                if (groups != null)
                {
                    if (groups.Contains(groupName))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查一组Group是否有差异,哪怕是配置文件有差异，也算有更新
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool CheckGroupsHasUpdate(List<string> groupsName)
        {
            if (groupsName == null)
                return false;
            for (int i = 0; i < groupsName.Count; i++)
            {
                if (CheckGroupHasUpdate(groupsName[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取单个Group更新的资源长度
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public ulong GetGroupLength(string groupName)
        {
            ulong length = 0u;
            if (string.IsNullOrEmpty(groupName)) return length;
            foreach (var kv in _cacheCheckInfoDic)
            {
                CheckInfo checkInfo = kv.Value;

                List<string> groups = checkInfo.GetGroups();
                if (groups != null)
                {
                    if (groups.Contains(groupName))
                    {
                        if (checkInfo.CheckNewIn == CheckNewIn.Remote)
                        {
                            length += checkInfo.RemoteManifest.Length;
                        }
                        else
                        {
                            length += manifestLength;
                        }
                    }
                }
            }
            return length;
        }

        /// <summary>
        /// 检查一组资源组的更新长度
        /// </summary>
        /// <param name="groupsName"></param>
        /// <returns></returns>
        public ulong GetGroupLength(List<string> groupsName)
        {
            ulong length = 0u;
            if (groupsName == null)
                return length;
            foreach (var kv in _cacheCheckInfoDic)
            {
                CheckInfo checkInfo = kv.Value;

                List<string> groups = checkInfo.GetGroups();
                if (groups != null)
                {
                    for (int i = 0; i < groupsName.Count; i++)
                    {
                        if (groups.Contains(groupsName[i]))
                        {
                            if (checkInfo.CheckNewIn == CheckNewIn.Remote)
                            {
                                length += checkInfo.RemoteManifest.Length;
                            }
                            else
                            {
                                length += manifestLength;
                            }
                            break;
                        }
                    }
                }
            }

            return length;
        }

        internal List<CheckInfo> GetCheckInfoByGroups(List<string> groupsName)
        {
            if (groupsName == null)
                return null;
            List<CheckInfo> result = new List<CheckInfo>();
            foreach (var kv in _cacheCheckInfoDic)
            {
                CheckInfo checkInfo = kv.Value;
                List<string> groups = checkInfo.GetGroups();
                if (checkInfo.CheckNewIn == CheckNewIn.Dispose)
                    result.Add(checkInfo);
                else
                {
                    if (groups != null)
                    {
                        for (int i = 0; i < groupsName.Count; i++)
                        {
                            if (groups.Contains(groupsName[i]))
                            {
                                result.Add(checkInfo);
                                break;
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
