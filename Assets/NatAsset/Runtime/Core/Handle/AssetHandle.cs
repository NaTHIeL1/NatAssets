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
            SetProvider(null);
            Asset = obj;
            _handleResult = Asset != null ? HandleResult.Success : HandleResult.Faild;
            _handleState = HandleState.Finish;
            if (CheckTokenCancel())
            {
                return;
            }

            CheckError();
            OnLoadedCallback?.Invoke(this);
            AsyncStateCallback?.Invoke();
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
                //无效句柄，已被回收或丢弃
                Debug.LogWarning("无效句柄");
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
            AssetInfo assetInfo = RuntimeData.GetAsset(Name);
            if (assetInfo != null)
            {
                assetInfo.RedRefCount();
            }
        }

        protected override void OnClear()
        {
            Asset = null;
            TargetType = null;
            OnLoadedCallback = null;
        }

        public HandleAwait<AssetHandle> GetAwaiter()
        {
            return new HandleAwait<AssetHandle>(this);
        }
    }
}