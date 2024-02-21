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
            string[] versionStr = version.Split('-');
            VersionData versionData = new VersionData();
            versionData.Year = Int32.Parse(versionStr[0]);
            versionData.Month = Int32.Parse(versionStr[1]);
            versionData.Day = Int32.Parse(versionStr[2]);
            versionData.Hour = Int32.Parse(versionStr[3]);
            versionData.Minutes = Int32.Parse(versionStr[4]);
            return versionData;
        }

    }


    public struct VersionData
    {
        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minutes;

        public static VersionData NowVersion()
        {
            VersionData versionData = new VersionData();
            versionData.Year = DateTime.Now.Year;
            versionData.Month = DateTime.Now.Month;
            versionData.Day = DateTime.Now.Day;
            versionData.Hour = DateTime.Now.Hour;
            versionData.Minutes = DateTime.Now.Minute;
            return versionData;
        }

        public static VersionData ConvertToVersionData(string version)
        {
            string[] versionStr = version.Split('-');
            VersionData versionData = new VersionData();
            versionData.Year = Int32.Parse(versionStr[0]);
            versionData.Month = Int32.Parse(versionStr[1]);
            versionData.Day = Int32.Parse(versionStr[2]);
            versionData.Hour = Int32.Parse(versionStr[3]);
            versionData.Minutes = Int32.Parse(versionStr[4]);
            return versionData;
        }

        public static bool operator >(VersionData a, VersionData b)
        {
            if (a.Year > b.Year) return true;
            if (a.Month > b.Month) return true;
            if (a.Day > b.Day) return true;
            if (a.Hour > b.Hour) return true;
            if (a.Minutes > b.Minutes) return true;
            return false;
        }

        public static bool operator <(VersionData a, VersionData b)
        {
            if (a.Year < b.Year) return true;
            if (a.Month < b.Month) return true;
            if (a.Day < b.Day) return true;
            if (a.Hour < b.Hour) return true;
            if (a.Minutes < b.Minutes) return true;
            return false;
        }

        public static bool operator ==(VersionData a, VersionData b)
        {
            if (a.Year == b.Year && a.Month == b.Month && a.Day == b.Day && a.Hour == b.Hour && a.Minutes == b.Minutes) return true;
            return false;
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