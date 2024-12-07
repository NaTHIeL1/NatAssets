using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    internal class UpdateHandle : IHandle
    {
        private GroupDownLoadProvider _downLoadProvider;
        private Action<UpdateHandle> OnUpdatedCallback;
        private List<string> _failList;
        private bool _isSuccess = false;
        private bool _isComplete = false;

        public bool DownLoadSuccess => _isSuccess;
        public bool Complete => _isComplete;
        public List<string> FailList => _failList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UpdateHandle Create(string name)
        {
            UpdateHandle assetHandle = ReferencePool.Get<UpdateHandle>();
            assetHandle.CreateHandle(name);
            assetHandle._isComplete = false;
            return assetHandle;
        }

        internal void SetResult(GroupDownLoadProvider groupDownLoadProvider)
        {
            _isComplete = true;
            if (groupDownLoadProvider.ProviderResult == ProviderResult.Faild)
            {
                _isSuccess = false;
                _failList = groupDownLoadProvider.FailDownLoadBundle;
            }
            else
            {
                _isSuccess = true;
            }
            OnUpdatedCallback?.Invoke(this);
        }

        protected override void OnSetProvider(BaseProvider baseProvider)
        {
            base.OnSetProvider(baseProvider);
            if(baseProvider == null)
                _downLoadProvider = null;
            else
                _downLoadProvider = baseProvider as GroupDownLoadProvider;
        }


        protected override void OnClear()
        {
            _downLoadProvider = null;
            OnUpdatedCallback = null;
            _isComplete = false;
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
            _isComplete = true;
            CommonHandleReleaseLogic();
        }

        protected override bool CanBeReference()
        {
            return false;
        }

        public ulong DownLoadRate()
        {
            if(_downLoadProvider == null)
                return 0;
            return  _downLoadProvider.DownLoadRate;
        }

        public ulong DownLoadLength()
        {
            if(_downLoadProvider != null)
                return 0;
            return _downLoadProvider.DownLoadLength;
        }

        public event Action<UpdateHandle> OnUpdated
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
