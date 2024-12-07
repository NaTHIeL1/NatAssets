using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class UpdateHandles : IHandle
    {
        private List<UpdateHandle> _updateHandles = new List<UpdateHandle>();
        private GroupDownLoadProvider _downLoadProvider;
        private List<string> _failList;

        private Action<List<UpdateHandle>> OnUpdatedCallback;

        public List<string> FailList => _failList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UpdateHandles Create(string name)
        {
            UpdateHandles assetHandle = ReferencePool.Get<UpdateHandles>();
            assetHandle.CreateHandle(name);
            return assetHandle;
        }

        protected override void OnClear()
        {
            _updateHandles.Clear();
            _downLoadProvider = null;
            OnUpdatedCallback = null;
        }

        protected override void OnDispose()
        {
            if (!IsValid)
            {
                //无效句柄，已被回收或丢弃
                Debug.LogWarning("无效句柄");
                return;
            }
            else if(_handleState == HandleState.Loading)
            {
                Cancel();
            }
        }

        protected override bool CanBeReference()
        {
            return false;
        }

        //todo:完成的判断条件需要重新修订
        public event Action<List<UpdateHandle>> OnUpdated
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
                    value?.Invoke(_updateHandles);
                    return;
                }
                OnUpdatedCallback += value;
            }
            remove
            {
                if (!IsValid)
                {
                    Debug.LogWarning($"此句柄资源:{Name} 为无效句柄");
                    return;
                }
                OnUpdatedCallback -= value;
            }
        }
    }
}
