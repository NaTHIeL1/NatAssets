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
            SetProvider(null);
            SceneObj = sceneObj;
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
            OnLoadedCallback?.Invoke(this);
            AsyncStateCallback?.Invoke();
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
                    value?.Invoke(this);
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

        protected override void OnUnLoad()
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
            SceneInfo sceneInfo = RuntimeData.GetScene(SceneObj);
            if (sceneInfo == null) return;
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
        }

        public HandleAwait<SceneHandle> GetAwaiter()
        {
            return new HandleAwait<SceneHandle>();
        }
    }
}