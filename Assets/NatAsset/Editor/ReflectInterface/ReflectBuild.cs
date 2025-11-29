using System;
using System.Collections.Generic;
using System.Linq;

namespace NATFrameWork.NatAsset.Editor
{
    public class ReflectBuild
    {
        // public static JSONNode ReflectBuildConfig()
        // {
        //     IEnumerable<Type> typeSearchs = new List<Type>();
        //
        //     var test = AppDomain.CurrentDomain.GetAssemblies();
        //     for (int i = 0; i < test.Length; i++)
        //     {
        //         var tempRes = test[i].GetTypes()
        //             .Where((t => typeof(OnBeforeBuildConfig).IsAssignableFrom(t)))
        //             .Where(t => !t.IsAbstract && t.IsClass);
        //         if (tempRes == null || tempRes.Count() == 0)
        //             continue;
        //
        //         typeSearchs = typeSearchs.Concat(tempRes);
        //     }
        //
        //     JSONNode tempJson = new JSONObject();
        //     foreach (var type in typeSearchs)
        //     {
        //         OnBeforeBuildConfig onBeforeBuildConfig = (OnBeforeBuildConfig) Activator.CreateInstance(type);
        //         JSONNode valueNode = onBeforeBuildConfig.BeforeBuildConfigFile(out string keyName);
        //         tempJson.Add(keyName, valueNode);
        //     }
        //
        //     return tempJson;
        // }

        //排除目标文件夹
        public static List<string> ReflectExcludeFileExtension()
        {
            IEnumerable<Type> typeSearchs = new List<Type>();

            var test = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < test.Length; i++)
            {
                var tempRes = test[i].GetTypes()
                    .Where((t => typeof(OnExcludeFilePath).IsAssignableFrom(t)))
                    .Where(t => !t.IsAbstract && t.IsClass);
                if (tempRes == null || tempRes.Count() == 0)
                    continue;

                typeSearchs = typeSearchs.Concat(tempRes);
            }

            List<string> excludeFile = new List<string>();
            foreach (var type in typeSearchs)
            {
                OnExcludeFilePath onExcludeFilePath = (OnExcludeFilePath)Activator.CreateInstance(type);
                excludeFile.AddRange(onExcludeFilePath.GenerateExcludeFilName());
            }

            //标准化格式
            for (int i = 0; i < excludeFile.Count; i++)
            {
                excludeFile[i] = excludeFile[i].Replace(@"\", "/").ToLower();
            }

            return excludeFile;
        }

        public static bool BeforeBuildExtension()
        {
            IEnumerable<Type> typeSearchs = new List<Type>();

            var test = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < test.Length; i++)
            {
                var tempRes = test[i].GetTypes()
                    .Where((t => typeof(OnBeforeBuildExe).IsAssignableFrom(t)))
                    .Where(t => !t.IsAbstract && t.IsClass);
                if (tempRes == null || tempRes.Count() == 0)
                    continue;

                typeSearchs = typeSearchs.Concat(tempRes);
            }

            List<OnBeforeBuildExe> temp = new List<OnBeforeBuildExe>();
            foreach (var type in typeSearchs)
            {
                OnBeforeBuildExe onBeforeBuildExe = (OnBeforeBuildExe)Activator.CreateInstance(type);
                temp.Add(onBeforeBuildExe);
            }

            temp.Sort((x, y) => { return x.Priority.CompareTo(y.Priority); });

            try
            {
                for (int i = 0; i < temp.Count; i++)
                {
                    temp[i].ExecutBeforBuild();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            return true;
        }

        //指定文件夹的Bundle策略
        //public static List<string> ReflectGenerateBundleAToBExtension()
        //{
        //    IEnumerable<Type> typeSearchs = new List<Type>();

        //    var test = AppDomain.CurrentDomain.GetAssemblies();
        //    for (int i = 0; i < test.Length; i++)
        //    {
        //        var tempRes = test[i].GetTypes()
        //            .Where((t => typeof(IFileToAssetAsBundle).IsAssignableFrom(t)))
        //            .Where(t => !t.IsAbstract && t.IsClass);
        //        if (tempRes == null || tempRes.Count() == 0)
        //            continue;

        //        typeSearchs = typeSearchs.Concat(tempRes);
        //    }

        //    List<string> stargeFiles = new List<string>();
        //    foreach (var type in typeSearchs)
        //    {
        //        IFileToAssetAsBundle fileToAssetAsBundle = (IFileToAssetAsBundle) Activator.CreateInstance(type);
        //        stargeFiles.AddRange(fileToAssetAsBundle.GenerateBundleAToB());
        //    }

        //    //标准化格式
        //    for (int i = 0; i < stargeFiles.Count; i++)
        //    {
        //        stargeFiles[i] = stargeFiles[i].Replace(@"\", "/");
        //    }

        //    return stargeFiles;
        //}

        // public static TRes ReflectMethod<T, TRes>() where T:OnReflectExtend
        // {
        //     IEnumerable<Type> typeSearchs = new List<Type>();
        //
        //     var test = AppDomain.CurrentDomain.GetAssemblies();
        //     for (int i = 0; i < test.Length; i++)
        //     {
        //         var tempRes = test[i].GetTypes()
        //             .Where((t => typeof(T).IsAssignableFrom(t)))
        //             .Where(t => !t.IsAbstract && t.IsClass);
        //         if (tempRes == null || tempRes.Count() == 0)
        //             continue;
        //
        //         typeSearchs = typeSearchs.Concat(tempRes);
        //     }
        //
        //     List<string> excludeFile = new List<string>();
        //     foreach (var type in typeSearchs)
        //     {
        //         T onExcludeFilePath = (T) Activator.CreateInstance(type);
        //         onExcludeFilePath.ReflectMethod<TRes>();
        //     }
        //     
        //     return excludeFile;
        // } 
    }
}