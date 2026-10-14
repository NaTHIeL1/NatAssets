using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    public class SceneHandle : IHandle
    {
        public Scene SceneObj { get; internal set; }


        private LoadSceneMode _loadSceneMode = LoadSceneMode.Single;
        private SceneInfo _ownedScene;
        private long _ownedGeneration;

        internal Action<SceneHandle> OnLoadedCallback;
        public bool IsMainScene => _loadSceneMode == LoadSceneMode.Single;

        public float Process
        {
            get { return 1; }
        }

        /// <summary>
        /// 返回设置是否成功
        /// </summary>
        /// <returns></returns>
        public bool SetSceneActive()
        {
            if (!IsValid)
                return false;
            if (SceneObj.IsValid() && SceneObj.isLoaded)
            {
                return SceneManager.SetActiveScene(SceneObj);
            }
            else
            {
                Debug.LogError($"场景:{SceneObj.path}未加载完成，无法设置");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static SceneHandle Create(string name, LoadSceneMode loadSceneMode)
        {
            SceneHandle sceneHandle = ReferencePool.Get<SceneHandle>();
            sceneHandle.CreateHandle(name);
            sceneHandle._loadSceneMode = loadSceneMode;
            return sceneHandle;
        }

        internal void SetScene(Scene sceneObj)
        {
            if (!IsValid || IsDone) return;
            SetProvider(null);
            SceneObj = sceneObj;
            _ownedScene = RuntimeData.GetScene(sceneObj);
            _ownedGeneration = _ownedScene?.Generation ?? 0;
            if (SceneObj == default)
            {
                _handleResult = HandleResult.Faild;
            }
            else
            {
                if (SceneObj.IsValid() && SceneObj.isLoaded)
                {
                    _handleResult = HandleResult.Success;
                }
                else
                {
                    _handleResult = HandleResult.Nono;
                }
            }

            _handleState = HandleState.Finish;
            if (CheckTokenCancel())
                return;
            CheckError();
            var callbacks = OnLoadedCallback;
            var continuations = AsyncStateCallback;
            string context = Name;
            OnLoadedCallback = null;
            AsyncStateCallback = null;
            CompletionCallbacks.Invoke(callbacks, this, context);
            CompletionCallbacks.Invoke(continuations, context);
        }

        public event Action<SceneHandle> OnLoaded
        {
            add
            {
                if (!IsValid)
                {
                    Debug.LogWarning($"此句柄资源:{Name} 为无效句柄");
                    return;
                }
                else if (IsDone)
                {
                    CompletionCallbacks.Invoke(value, this, Name);
                    return;
                }

                OnLoadedCallback += value;
            }
            remove
            {
                if (!IsValid)
                {
                    Debug.LogWarning($"此句柄资源:{Name} 为无效句柄");
                    return;
                }

                OnLoadedCallback -= value;
            }
        }

        protected override void OnDispose()
        {
            if (!IsValid)
            {
                Debug.LogWarning("无效句柄");
                return;
            }

            if (IsMainScene)
            {
                Debug.LogError("主场景不能通过Unload接口卸载");
                return;
            }

            ReleaseLogic();
        }

        internal void ReleaseLogic()
        {
            InternalUnLoad();
            CommonHandleReleaseLogic();
        }

        private void InternalUnLoad()
        {
            SceneInfo sceneInfo = _ownedScene;
            _ownedScene = null;
            if (sceneInfo == null || sceneInfo.Generation != _ownedGeneration || RuntimeData.IsReleasing) return;
            sceneInfo.RedRefCount();
        }

        protected override bool CanBeReference()
        {
            return false;
        }

        protected override void OnClear()
        {
            SceneObj = default;
            OnLoadedCallback = null;
            _ownedScene = null;
            _ownedGeneration = 0;
        }

        public HandleAwait<SceneHandle> GetAwaiter()
        {
            return new HandleAwait<SceneHandle>(this);
        }
    }
}
