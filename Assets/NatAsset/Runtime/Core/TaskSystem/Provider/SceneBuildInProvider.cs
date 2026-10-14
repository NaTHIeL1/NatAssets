using UnityEngine;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class SceneBuildInProvider : BaseProvider
    {
        private AsyncOperation _operation;
        private Scene _scene;
        private string _sceneName;
        private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
        private SceneProviderParam _sceneProviderParam;

        protected override void OnCreate()
        {
            _sceneProviderParam = (SceneProviderParam)_providerParam;
            _loadSceneMode = _sceneProviderParam.LoadSceneMode;
            _isSceneProvider = true;
            RuntimeData.GetSceneName(_assetPath, out _sceneName);
        }

        internal override void OnUpdate()
        {
            //未启动任务
            if (ProviderState == ProviderState.Waiting)
            {
                CommonFunc.LoadScene(AssetPath, Priority, RunModel, _loadSceneMode, out _scene, out _operation);
                SetSceneToHandle(_scene);
                SetProviderState(ProviderState.Running);
            }

            //任务执行中
            if (ProviderState == ProviderState.Running)
            {
                //operation为空代表加载失败或使用了同步加载，scene=default代表加载失败
                if (_operation == null)
                {
                    SetProviderState(ProviderState.Finish);
                    SetSceneSetting(_scene, false);
                }

                if (_operation != null && _operation.isDone)
                {
                    SetProviderState(ProviderState.Finish);
                    SetSceneSetting(_scene, false);
                }
            }
        }

        protected override void OnClear()
        {
            SetOwnerAndDependRelease();
            _operation = null;
            _scene = default;
            _sceneName = null;
            _isSceneProvider = false;
            _loadSceneMode = LoadSceneMode.Single;
            _sceneProviderParam = default;
        }

        protected override void OnCancelProvider()
        {
            if (ProviderState == ProviderState.Waiting)
            {
                SetProviderState(ProviderState.Finish);
            }
        }

        protected override bool CanChangeLoadType()
        {
            return false;
        }

        protected override void OnChangeLoadType(RunModel runModel)
        {
        }

        protected override void OnChangePriority(Priority targetPriority)
        {
            if (ProviderState == ProviderState.Running)
            {
                SetOwnerAndDependPriority(targetPriority);
            }
        }

        private void SetSceneSetting(Scene scene, bool isRelease)
        {
            string error = string.Empty;
            if (scene == default)
            {
                error = $"场景路径:{AssetPath},加载场景:{_sceneName}时出错，检查场景路径是否正确";
                SetProviderResult(ProviderResult.Faild);
                SetSceneHandle(scene, error, null);
                return;
            }

            SceneInfo sceneInfo = SceneInfo.CreateSceneInfo(_providerGUID, scene, null);
            RuntimeData.AddSceneInfo(sceneInfo);
            SetProviderResult(ProviderResult.Success);
            SetSceneHandle(scene, error, sceneInfo);
        }

        private void SetSceneHandle(Scene scene, string error, SceneInfo sceneInfo)
        {
            if (error != string.Empty)
            {
                Debug.LogError(error);
            }

            if (Handles == null || Handles.Count == 0)
            {
                if (sceneInfo == null) return;
                sceneInfo.AddRefCount();
                sceneInfo.RedRefCount();
            }
            else
            {
                foreach (var handle in Handles.ToArray())
                {
                    SceneHandle sceneHandle = handle as SceneHandle;
                    if (sceneHandle == null || !sceneHandle.IsValid || sceneHandle.IsDone) continue;
                    sceneInfo?.AddRefCount();
                    sceneHandle.Error = error;
                    sceneHandle.SetScene(scene);
                }
            }
        }
        
        private void SetSceneToHandle(Scene scene)
        {
            if (Handles == null)
                return;
            for (int i = 0; i < Handles.Count; i++)
            {
                SceneHandle sceneHandle = Handles[i] as SceneHandle;
                sceneHandle.SceneObj = scene;
            }
        }
    }
}