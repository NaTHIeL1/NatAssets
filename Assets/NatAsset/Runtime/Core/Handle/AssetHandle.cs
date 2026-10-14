using System;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace NATFrameWork.NatAsset.Runtime
{
    public class AssetHandle : IHandle
    {
        public object Asset { get; protected set; }
        public Type TargetType { get; protected set; }

        private Action<AssetHandle> OnLoadedCallback;
        private AssetInfo _ownedAsset;
        private long _ownedGeneration;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static AssetHandle Create(string name, Type type)
        {
            AssetHandle assetHandle = ReferencePool.Get<AssetHandle>();
            assetHandle.CreateHandle(name);
            assetHandle.TargetType = type;
            return assetHandle;
        }

        internal void SetAsset(object obj)
        {
            if (!IsValid || IsDone) return;
            SetProvider(null);
            Asset = obj;
            _ownedAsset = obj == null ? null : RuntimeData.GetAsset(Name, TargetType);
            _ownedGeneration = _ownedAsset?.Generation ?? 0;
            _handleResult = Asset != null ? HandleResult.Success : HandleResult.Faild;
            _handleState = HandleState.Finish;
            if (CheckTokenCancel())
            {
                return;
            }

            CheckError();
            var callbacks = OnLoadedCallback;
            var continuations = AsyncStateCallback;
            string context = Name;
            OnLoadedCallback = null;
            AsyncStateCallback = null;
            CompletionCallbacks.Invoke(callbacks, this, context);
            CompletionCallbacks.Invoke(continuations, context);
        }

        public event Action<AssetHandle> OnLoaded
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

            // else if (_handleState == HandleState.Finish)
            // {
            //     if (_handleResult == HandleResult.Success)
            //     {
            //         
            //     }
            // }
            InternalUnLoad();
            CommonHandleReleaseLogic();
        }

        private void InternalUnLoad()
        {
            //已经加载完毕的走计数卸载流程
            AssetInfo assetInfo = _ownedAsset;
            _ownedAsset = null;
            if (assetInfo != null && assetInfo.Generation == _ownedGeneration && !RuntimeData.IsReleasing)
            {
                assetInfo.RedRefCount();
            }
        }

        protected override void OnClear()
        {
            Asset = null;
            TargetType = null;
            OnLoadedCallback = null;
            _ownedAsset = null;
            _ownedGeneration = 0;
        }

        public HandleAwait<AssetHandle> GetAwaiter()
        {
            return new HandleAwait<AssetHandle>(this);
        }
    }
}
