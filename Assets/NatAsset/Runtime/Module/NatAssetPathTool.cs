using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace NATFrameWork.NatAsset.Runtime
{
    internal static class NatAssetPathTool
    {
        //随包资源目录
        private const string _buildInFolder = "BuildInRes";
        //读写资源目录
        private const string _sandBoxFolder = "SandBox";
        //资源下载目录
        private const string _tempCacheFolder = "TempCache";

        private static string _readWritePath;
        private static string _readOnlyPath;
        private static string _remotePath;
        private static string _cacheDownLoadPath;

#if UNITY_EDITOR
        //该路径用于编辑器下的Bundle模拟
        internal const string SPECIAL_EditorFile = "Editor";
        private static string _editorSimulationPath;
        private static string EditorSimulationPath
        {
            get
            {
                if (string.IsNullOrEmpty(_editorSimulationPath))
                {
                    //编辑器下反射获取加载路径
                    Assembly natAssetEditor = AppDomain.CurrentDomain.GetAssemblies()
                        .First(assembly => assembly.GetName().Name == "NatAssetEditor");
                    IEnumerable<Type> typeSearchs = new List<Type>();
                    var tempRes = natAssetEditor.GetTypes()
                        .Where((t => typeof(IEditorReflect).IsAssignableFrom(t)))
                        .Where(t => !t.IsAbstract && t.IsClass);
                    typeSearchs = typeSearchs.Concat(tempRes);

                    foreach (var type in typeSearchs)
                    {
                        IEditorReflect editorReflect = (IEditorReflect)Activator.CreateInstance(type);
                        _editorSimulationPath = Path.Combine(Directory.GetCurrentDirectory(), editorReflect.GetOutPutPathByRuntime);
                    }
                }
                return _editorSimulationPath;
            }
        }
        
        private static string _editorLoadPathByRunway;
        internal static string EditorLoadPath
        {
            get
            {
                if (string.IsNullOrEmpty(_editorLoadPathByRunway))
                {
                    _editorLoadPathByRunway = Path.Combine(EditorSimulationPath, EditorUserBuildSettings.activeBuildTarget.ToString(), SPECIAL_EditorFile);
                }
                return _editorLoadPathByRunway;
            }
        }
#endif

        //只读路径
        internal static string ReadOnlyPath
        {
            get
            {
                if (string.IsNullOrEmpty(_readOnlyPath))
                {
                    string streamingPath = Path.Combine(Application.streamingAssetsPath, _buildInFolder);
                    _readOnlyPath = FileExtend.NormalizePath(streamingPath);
                    CreateDirectory(_readOnlyPath);
                }

                return _readOnlyPath;
            }
        }

        //可读写路径
        internal static string ReadWritePath
        {
            get
            {
                
#if UNITY_EDITOR
                //编辑器下模拟资源更新
                if (string.IsNullOrEmpty(_readWritePath))
                {
                    string path = Path.Combine(EditorLoadPath, _sandBoxFolder);
                    _readWritePath = FileExtend.NormalizePath(path);
                    CreateDirectory(_readWritePath);
                }
#endif
#if UNITY_STANDALONE
                if (string.IsNullOrEmpty(_readWritePath))
                {
                    string standPath = Path.Combine(Application.dataPath, _sandBoxFolder);
                    _readWritePath = FileExtend.NormalizePath(standPath);
                    CreateDirectory(_readWritePath);
                }
#else
                if (string.IsNullOrEmpty(_readWritePath))
                {
                    string otherPath = Path.Combine(Application.persistentDataPath, _sandBoxFolder);
                    _readWritePath = FileExtend.NormalizePath(otherPath);
                    CreateDirectory(_readWritePath);
                }
#endif
                return _readWritePath;
            }
        }

        //远端路径
        internal static string RemotePath
        {
            get
            {
                if (string.IsNullOrEmpty(_remotePath))
                {
                    if (string.IsNullOrEmpty(NatAssetSetting.AssetServerURL))
                    {
                        throw new Exception("未设置服务器远端资源地址，需要设置NatAssetMgr.UpdateURLAddress");
                    }
                    string url = NatAssetSetting.AssetServerURL;
                    _remotePath = url;
                    return url;
                }
                return _remotePath;
            }
        }

        internal static string CacheDownLoadPath
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(_cacheDownLoadPath))
                {
                    string downLoadPath = Path.Combine(EditorLoadPath, _tempCacheFolder);
                    _cacheDownLoadPath = FileExtend.NormalizePath(downLoadPath);
                    CreateDirectory(_cacheDownLoadPath);
                }
#endif

#if UNITY_STANDALONE
                if (string.IsNullOrEmpty(_cacheDownLoadPath))
                {
                    string downLoadPath = Path.Combine(Application.dataPath, _tempCacheFolder);
                    _cacheDownLoadPath = FileExtend.NormalizePath(downLoadPath);
                    CreateDirectory(_cacheDownLoadPath);
                }
#else
                if (string.IsNullOrEmpty(_cacheDownLoadPath))
                {
                    string downLoadPath = Path.Combine(Application.persistentDataPath, _tempCacheFolder);
                    _cacheDownLoadPath = FileExtend.NormalizePath(downLoadPath);
                    CreateDirectory(_cacheDownLoadPath);
                }
#endif
                return _cacheDownLoadPath;
            }
        }
        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}