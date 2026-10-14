using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace NATFrameWork.NatAsset.Runtime
{
    public class StandaloneModeLoader : BaseManifestModeLoader
    {
        private string _webTaskGUID = string.Empty;
        private string _buildInPath = string.Empty;
        private NatAssetManifest _buildInManifest;
        
#if UNITY_EDITOR
        [System.Serializable] private sealed class EditorSyncStatus { public bool committed; }
#endif
        private bool _stopped;
        private bool _completed;

        internal override void StartLoader()
        {
            _stopped = false;
            _completed = false;
            LastError = null;
            _buildInPath = Path.Combine(BuildInLoadPath, NatAssetSetting.ConfigName);
#if UNITY_EDITOR
            string syncJournal = BuildInLoadPath + ".sync.json";
            if (File.Exists(syncJournal) && !JsonUtility.FromJson<EditorSyncStatus>(File.ReadAllText(syncJournal)).committed)
            {
                Fail("编辑器测试资源同步尚未完成，请退出播放并打开 NatFramework/Build 恢复或重新应用资源。");
                return;
            }
            if (!File.Exists(_buildInPath))
            {
                Fail("编辑器 Bundle 测试清单不存在。请打开 NatFramework/Build，选择资源版本并应用到编辑器测试。");
                return;
            }
#endif
            _webTaskGUID = CoreLoaderMgr.SendWebRequest(_buildInPath, Priority.Top, CallBack);
        }

        private void CallBack(string taskId, bool success, UnityWebRequest request)
        {
            if (_stopped || _completed) return;
            _webTaskGUID = string.Empty;
            try
            {
                if (!success || request == null || request.result != UnityWebRequest.Result.Success)
                    throw new System.IO.IOException($"请求失败：{request?.error ?? "无有效请求"}；HTTP {request?.responseCode}");
                var bytes = request.downloadHandler?.data;
                _buildInManifest = ParseManifest(bytes, NatAssetSetting.ExpectedBundlePlatform);
#if UNITY_EDITOR
                foreach (var bundle in _buildInManifest.BundleManifests)
                {
                    var file = Path.Combine(BuildInLoadPath, bundle.RelativePath);
                    if (!File.Exists(file) || (ulong)new FileInfo(file).Length != bundle.Length)
                        throw new InvalidDataException($"编辑器测试资源缺失或长度不符：{file}。请重新应用资源版本。");
                }
#endif
                _buildInManifest.InitRunTimeManifest(LoadPath.ReadOnly);
                _completed = true;
                var callback = LoadCallBack;
                LoadCallBack = null;
                CompletionCallbacks.Invoke(callback, ManifestModeState.Finish, _buildInPath);
            }
            catch (System.Exception error) { Fail(error.Message); }
        }

        internal static NatAssetManifest ParseManifest(byte[] bytes, string platform)
        {
            var manifest = NatAssetManifest.DeserializeFromBinary(bytes);
            if (manifest == null || manifest.BundleManifests == null || manifest.BundleManifests.Count == 0)
                throw new InvalidDataException("资源清单为空或格式无效。");
            if (manifest.Platform != platform)
                throw new InvalidDataException($"资源平台 {manifest.Platform} 与运行平台 {platform} 不一致。");
            return manifest;
        }

        private void Fail(string reason)
        {
            if (_stopped || _completed) return;
            _completed = true;
            _buildInManifest = null;
            LastError = $"[NatAsset] 初始化失败：{_buildInPath}；{reason}";
            var callback = LoadCallBack;
            LoadCallBack = null;
            CompletionCallbacks.Invoke(callback, ManifestModeState.Error, _buildInPath);
        }

        internal override void StopLoader()
        {
            _stopped = true;
            LoadCallBack = null;
            CoreLoaderMgr.DisposeWebRequest(_webTaskGUID);
            _webTaskGUID = string.Empty;
            _buildInPath = string.Empty;
            _buildInManifest = null;
        }

        internal override BundleManifest GetBundleManifest(string bundlePath)
        {
            if(_buildInManifest != null)
            {
                return _buildInManifest.GetBundleManifest(bundlePath);
            }
            return null;
        }

        internal override AssetManifest GetAssetManifest(string assetPath)
        {
            if(_buildInManifest != null)
            {
                return _buildInManifest.GetAssetManifest(assetPath);
            }
            return null;
        }

        internal override List<string> GetBundleGroup(string groupName)
        {
            throw new System.NotImplementedException();
        }

        internal override Dictionary<string, BundleManifest> GetBundleManifestDic()
        {
            if (_buildInManifest != null)
                return _buildInManifest.BundleManifestDic;
            return null;
        }
    }
}
