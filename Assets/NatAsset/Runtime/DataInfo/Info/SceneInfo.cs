using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public class SceneInfo : BaseInfo
    {
        internal Scene Scene { get; private set; }
        internal List<string> DepBundles { get; private set; }

        private UnLoadSceneOperation _unLoadSceneOperation;
        public bool IsBuildInScene { get; private set; }

        private void CreateInfo(string sceneGUID, Scene scene, List<string> depBundles)
        {
            Scene = scene;
            DepBundles = depBundles;
            IsBuildInScene = SceneSystem.IsBuildInScene(sceneGUID);
        }

        internal static SceneInfo CreateSceneInfo(string sceneGUID, Scene scene, List<string> depBundles)
        {
            SceneInfo sceneInfo = Create<SceneInfo>(sceneGUID);
            sceneInfo.CreateInfo(sceneGUID, scene, depBundles);
            return sceneInfo;
        }

        protected override void OnAddRefCount()
        {
            return;
        }

        protected override void OnRedRefCount()
        {
            if (CheckNeedUnLoadInfo())
            {
                LaunchUnLoadTask();
            }
        }

        protected override void OnClear()
        {
#if UNITY_EDITOR
            if (NatAssetSetting.TRunWay == RunWay.PackageOnly)
            {
                OnToReleaseScene();
            }
#else
            OnToReleaseScene();
#endif
            Scene = default;
            DepBundles = null;
        }

        private void LaunchUnLoadTask()
        {
            if (Scene.IsValid() && Scene.isLoaded)
            {
                _unLoadSceneOperation = UnLoadSceneOperation.Create(Scene);
                SceneSystem.AddOperation(_unLoadSceneOperation);
            }
        }

        private void OnToReleaseScene()
        {
            //内置场景资源直接返回
            if (IsBuildInScene) return;
            if (DepBundles != null)
            {
                foreach (string bundle in DepBundles)
                {
                    RuntimeData.GetBundle(bundle).RedRefCount();
                }
            }
        }
    }
}