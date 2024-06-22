using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class SceneBundleProvider : BaseProvider
    {
        private enum SceneProviderState
        {
            Nono,
            Cancel,
        }

        private TaskRunner _loadTaskRunner;
        private AsyncOperation _operation;
        private Scene _scene;
        private string _sceneName, _bundleName;
        private string[] _depBundles;
        private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
        private SceneProviderState _sceneProviderState = SceneProviderState.Nono;

        private SceneProviderParam _sceneProviderParam;

        protected override void OnCreate()
        {
            _sceneProviderParam = (SceneProviderParam)_providerParam;
            _loadSceneMode = _sceneProviderParam.LoadSceneMode;
            _isSceneProvider = true;
            _loadTaskRunner = TaskSystem.LoadTaskRunner;

            RuntimeData.GetBundleScenePath(AssetPath, out _bundleName, out _sceneName);
            _depBundles = RuntimeData.GetAllDependencies(_bundleName);
        }

        internal override void OnUpdate()
        {
            //未执行
            if (ProviderState == ProviderState.Waiting)
            {
                CommonBundleTaskLogic(_bundleName, _loadTaskRunner, Priority, out _OwnerMainTask);
                foreach (string bundle in _depBundles)
                {
                    BaseTask tempBaseTask = null;
                    if (CommonBundleTaskLogic(bundle, _loadTaskRunner, Priority, out tempBaseTask))
                    {
                        _DependenceTask.AddTask(tempBaseTask);
                    }
                }

                SetOwnerAndDependPriority(Priority);
                SetOwnerAndDependRunModel(RunModel);
                SetProviderState(ProviderState.Running);
            }

            //执行中
            if (ProviderState == ProviderState.Running)
            {
                if (!IsDoneOwnerAndDepend())
                    return;
                if (!IsSuccessOwnerAndDepend())
                {
                    SetSceneSetting(default, false);
                    SetProviderState(ProviderState.Finish);
                }
                else
                {
                    //未处于取消任务的状态
                    if (_sceneProviderState == SceneProviderState.Nono)
                    {
                        //任务未创建且
                        if (_operation == null)
                        {
                            CommonFunc.LoadScene(AssetPath, Priority, RunModel, _loadSceneMode, out _scene,
                                out _operation);
                            SetSceneToHandle(_scene);
                        }

                        //异步句柄为空代表可能加载失败或者同步加载，只需当做加载完成即可
                        if (_operation == null)
                        {
                            SetSceneSetting(_scene, false);
                            SetProviderState(ProviderState.Finish);
                        }

                        if (_operation != null && _operation.isDone)
                        {
                            if (_scene.IsValid() && _scene.isLoaded)
                            {
                                AddBundlesRefCount();
                            }

                            SetSceneSetting(_scene, false);
                            SetProviderState(ProviderState.Finish);
                        }
                    }
                    //处于取消任务的状态
                    else if (_sceneProviderState == SceneProviderState.Cancel)
                    {
                        if (_operation == null)
                        {
                            SetProviderState(ProviderState.Finish);
                        }
                        else
                        {
                            if (_operation.isDone)
                            {
                                if (_scene.IsValid() && _scene.isLoaded)
                                {
                                    AddBundlesRefCount();
                                }

                                SetSceneSetting(_scene, true);
                                SetProviderState(ProviderState.Finish);
                            }
                        }
                    }
                }
            }

            //执行完毕
            if (ProviderState == ProviderState.Finish)
            {
            }
        }

        protected override void OnClear()
        {
            //解除任务锁定
            SetOwnerAndDependRelease();
            _sceneProviderState = SceneProviderState.Nono;
            _scene = default;
            _operation = null;
            _isSceneProvider = false;
            _bundleName = null;
            _sceneName = null;
            _depBundles = null;
            _loadTaskRunner = null;
            _sceneProviderParam = default;
        }

        protected override bool CanChangeLoadType()
        {
            return false;
        }

        protected override void OnChangeLoadType(RunModel runModel)
        {
            return;
        }

        protected override void OnChangePriority(Priority targetPriority)
        {
            if (ProviderState == ProviderState.Running)
            {
                SetOwnerAndDependPriority(targetPriority);
                if (_operation != null)
                    _operation.priority = (int) targetPriority;
            }
        }

        /// <summary>
        /// 句柄全部移除时的取消逻辑
        /// </summary>
        protected override void OnCancelProvider()
        {
            _sceneProviderState = SceneProviderState.Cancel;
            //未执行直接打断
            if (ProviderState == ProviderState.Waiting)
            {
                SetProviderState(ProviderState.Finish);
            }
        }

        private void SetSceneSetting(Scene scene, bool isRelease)
        {
            string error = string.Empty;
            if (scene == default)
            {
                error = $"场景路径:{AssetPath},加载场景:{_sceneName}时出错，检查场景路径是否正确";
                SetSceneHandle(scene, error, null);
                SetProviderResult(ProviderResult.Faild);
                return;
            }

            List<string> bundles = new List<string>();
            bundles.Add(_bundleName);
            foreach (string bundle in _depBundles)
            {
                bundles.Add(bundle);
            }

            SceneInfo sceneInfo = SceneInfo.CreateSceneInfo(_providerGUID, scene, bundles);
            RuntimeData.AddSceneInfo(sceneInfo);
            SetSceneHandle(scene, error, sceneInfo);
            SetProviderResult(ProviderResult.Success);
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
                for (int i = 0; i < Handles.Count; i++)
                {
                    if (sceneInfo != null)
                        sceneInfo.AddRefCount();
                    SceneHandle sceneHandle = Handles[i] as SceneHandle;
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

        private void AddBundlesRefCount()
        {
            if (_OwnerMainTask != null)
            {
                BundleInfo bundleInfo = RuntimeData.GetBundle(_bundleName);
                if (!ContainHasAddInfo(bundleInfo))
                    bundleInfo.AddRefCount();
            }

            if (_depBundles == null) return;
            foreach (string bundleStr in _depBundles)
            {
                if (_DependenceTask.ContainsKey(bundleStr))
                {
                    BundleInfo tempBundle = RuntimeData.GetBundle(bundleStr);
                    if (!ContainHasAddInfo(tempBundle))
                        tempBundle.AddRefCount();
                }
            }
        }
    }
}