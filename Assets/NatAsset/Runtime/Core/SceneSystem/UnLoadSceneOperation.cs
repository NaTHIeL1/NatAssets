using UnityEngine;
using UnityEngine.SceneManagement;

namespace NATFrameWork.NatAsset.Runtime
{
    internal enum OperationState
    {
        Waiting,
        Running,
        Finish,
    }

    internal class UnLoadSceneOperation : IReference
    {
        private Scene _scene;
        private AsyncOperation _operation;
        private OperationState _operationState;
        public float Progress { get; protected set; }
        public bool isDone => _operationState == OperationState.Finish;

        internal static UnLoadSceneOperation Create(Scene scene)
        {
            UnLoadSceneOperation unLoadSceneOperation = ReferencePool.Get<UnLoadSceneOperation>();
            unLoadSceneOperation._scene = scene;
            unLoadSceneOperation._operationState = OperationState.Waiting;
            unLoadSceneOperation.Progress = 0;
            return unLoadSceneOperation;
        }

        internal static void Release(UnLoadSceneOperation unLoadSceneOperation)
        {
            ReferencePool.Release(unLoadSceneOperation);
        }

        internal void Update()
        {
            if (_operationState == OperationState.Waiting)
            {
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    _operation = SceneManager.UnloadSceneAsync(_scene);
                    if (_operation == null)
                        SetOperationState(OperationState.Finish);
                    else
                    {
                        SetOperationState(OperationState.Running);
                    }
                }
            }

            if (_operationState == OperationState.Running)
            {
                Progress = _operation.progress;
                if (_operation.isDone)
                {
                    SetOperationState(OperationState.Finish);
                }
            }

            if (_operationState == OperationState.Finish)
            {
                ReleaseSceneInfo();
            }
        }

        private void SetOperationState(OperationState operationState)
        {
            _operationState = operationState;
        }

        public void Clear()
        {
            _scene = default;
            _operation = null;
        }

        private void ReleaseSceneInfo()
        {
            SceneInfo sceneInfo = RuntimeData.GetScene(_scene);
            RuntimeData.RemoveSceneInfo(sceneInfo);
            SceneInfo.Release(sceneInfo);
        }
    }
}