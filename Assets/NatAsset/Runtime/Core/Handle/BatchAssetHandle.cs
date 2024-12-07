using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace NATFrameWork.NatAsset.Runtime
{
    public class BatchAssetHandle : IHandle, IOperation
    {
        bool IOperation.IsDone { get => IsDone; set { } }
        private List<AssetHandle> _assetHandles = new List<AssetHandle>();
        private Action<List<AssetHandle>> OnLoadedCallback;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static BatchAssetHandle Create()
        {
            BatchAssetHandle assetHandle = ReferencePool.Get<BatchAssetHandle>();
            assetHandle.CreateHandle(nameof(BatchAssetHandle));
            OperationSystem.AddOperation(assetHandle);
            return assetHandle;
        }

        public void OnUpdate()
        {
            bool isComplete = true;
            for (int i = 0; i < _assetHandles.Count; i++)
            {
                if (!_assetHandles[i].IsDone)
                {
                    isComplete = false;
                    break;
                }
            }
            if (isComplete)
            {
                _handleState = HandleState.Finish;
                _handleResult = HandleResult.Success;
                OnLoadedCallback?.Invoke(_assetHandles);
            }
        }

        public event Action<List<AssetHandle>> OnLoaded
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
                    value?.Invoke(_assetHandles);
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

        protected override bool CanBeReference()
        {
            return false;
        }

        protected override void OnDispose()
        {
            if (!IsValid)
            {
                //无效句柄，已被回收或丢弃
                Debug.LogWarning("无效句柄");
                return;
            }
            else if (_handleState == HandleState.Loading)
            {
                Cancel();
            }

            for (int i = 0; i < _assetHandles.Count; i++)
            {
                AssetHandle assetHandle = _assetHandles[i];
                assetHandle.Dispose();
            }
            CommonHandleReleaseLogic();
            OperationSystem.RemoveOperation(this);
        }

        protected override void OnClear()
        {
            _assetHandles.Clear();
            OnLoadedCallback = null;
        }

        public void AddAssetHandle(AssetHandle assetHandle)
        {
            _assetHandles.Add(assetHandle);
        }

        public void RemoveAssetHandle(AssetHandle assetHandle)
        {
            _assetHandles.Remove(assetHandle);
        }
    }
}
