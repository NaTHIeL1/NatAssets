using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public class SceneInfo : BaseInfo
    {
        internal Scene Scene { get; private set; }
        internal List<string> DepBundles { get; private set; }

        private UnLoadSceneOperation _unLoadSceneOperation;
        internal AssetLifetimeInfo Lifetime;
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
            RuntimeData.ReleaseSceneLifetime(Lifetime);
            Lifetime = null;
            Scene = default;
            DepBundles = null;
            _unLoadSceneOperation = null;
            IsBuildInScene = false;
        }

        private void LaunchUnLoadTask()
        {
            if (_unLoadSceneOperation == null && Scene.IsValid() && Scene.isLoaded)
            {
                _unLoadSceneOperation = UnLoadSceneOperation.Create(Scene);
                SceneSystem.AddOperation(_unLoadSceneOperation);
            }
        }

    }
}
