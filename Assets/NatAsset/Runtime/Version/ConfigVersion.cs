using System;

namespace NATFrameWork.NatAsset.Runtime
{
    public static class ConfigVersion
    {
        public static bool CheckNatAssetVersion(NatAssetManifest manifest)
        {
            if (manifest == null) return false;
            else
            {
                if (manifest.NatAssetVersion == NatAssetSetting.NatAssetVersion)
                {
                    return true;
                }
                else
                {
                    throw new Exception("配置文件版本与代码不匹配");
                }
            }
        }

        public static bool CompareManifest(NatAssetManifest manifest1, NatAssetManifest manifest2)
        {
            return CompareVersion(manifest1.ReleaseVersion, manifest2.ReleaseVersion);
        }

        /// <summary>
        /// 版本号格式 2023-7-7-xxx分钟,Release版本号右侧大于等于左侧返回true
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        private static bool CompareVersion(string version1, string version2)
        {
            VersionData versionData1 = ConvertToVersionData(version1);
            VersionData versionData2 = ConvertToVersionData(version2);
            return versionData1 <= versionData2;
        }
        
        /// <summary>
        /// string转日期结构体
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private static VersionData ConvertToVersionData(string version)
        {
            return VersionData.ConvertToVersionData(version);
        }

    }


    public struct VersionData : IComparable<VersionData>, IEquatable<VersionData>
    {
        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minutes;

        public static VersionData NowVersion()
        {
            DateTime now = DateTime.Now;
            VersionData versionData = new VersionData();
            versionData.Year = now.Year;
            versionData.Month = now.Month;
            versionData.Day = now.Day;
            versionData.Hour = now.Hour;
            versionData.Minutes = now.Minute;
            return versionData;
        }

        public static VersionData ConvertToVersionData(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new FormatException("资源版本不能为空。");
            string[] versionStr = version.Split('-');
            if (versionStr.Length != 5)
                throw new FormatException("资源版本格式必须为 yyyy-MM-dd-HH-mm。");
            VersionData versionData = new VersionData();
            versionData.Year = Int32.Parse(versionStr[0]);
            versionData.Month = Int32.Parse(versionStr[1]);
            versionData.Day = Int32.Parse(versionStr[2]);
            versionData.Hour = Int32.Parse(versionStr[3]);
            versionData.Minutes = Int32.Parse(versionStr[4]);
            // Validate the calendar as well as the number of version components.
            _ = new DateTime(versionData.Year, versionData.Month, versionData.Day,
                versionData.Hour, versionData.Minutes, 0);
            return versionData;
        }

        public int CompareTo(VersionData other)
        {
            int result = Year.CompareTo(other.Year);
            if (result != 0) return result;
            result = Month.CompareTo(other.Month);
            if (result != 0) return result;
            result = Day.CompareTo(other.Day);
            if (result != 0) return result;
            result = Hour.CompareTo(other.Hour);
            return result != 0 ? result : Minutes.CompareTo(other.Minutes);
        }

        public bool Equals(VersionData other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is VersionData other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Year;
                hash = hash * 397 ^ Month;
                hash = hash * 397 ^ Day;
                hash = hash * 397 ^ Hour;
                return hash * 397 ^ Minutes;
            }
        }

        public static bool operator >(VersionData a, VersionData b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(VersionData a, VersionData b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator ==(VersionData a, VersionData b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(VersionData a, VersionData b)
        {
            return !(a == b);
        }

        public static bool operator <=(VersionData a, VersionData b)
        {
            if (a == b) return true;
            if (a < b) return true;
            return false;
        }

        public static bool operator >=(VersionData a, VersionData b)
        {
            if (a == b) return true;
            if (a > b) return true;
            return false;
        }

        public override string ToString()
        {
            return string.Concat(Year, '-', Month.ToString("D2"), "-", Day.ToString("D2"), '-', Hour.ToString("D2"), '-', Minutes.ToString("D2"));
        }
    }
}
