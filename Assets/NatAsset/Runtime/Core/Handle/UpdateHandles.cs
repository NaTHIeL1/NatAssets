using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public class UpdateHandles : IHandle,IOperation
    {
        private List<UpdateHandle> _updateHandles = new List<UpdateHandle>();
        private GroupDownLoadProvider _downLoadProvider;
        private List<string> _failList;
        bool IOperation.IsDone
        {
            get => IsDone;
            set { }
        }
        private Action<List<UpdateHandle>> OnUpdatedCallback;

        public List<string> FailList => _failList;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UpdateHandles Create()
        {
            UpdateHandles updateHandles = ReferencePool.Get<UpdateHandles>();
            updateHandles.CreateHandle(nameof(UpdateHandles));
            OperationSystem.AddOperation(updateHandles);
            return updateHandles;
        }
        
        public void OnUpdate()
        {
            bool isComplete = true;
            
            for (int i = 0; i < _updateHandles.Count; i++)
            {
                if (!_updateHandles[i].IsDone)
                {
                    isComplete = false;
                    break;
                }
            }
            if (isComplete)
            {
                _handleState = HandleState.Finish;
                _handleResult = HandleResult.Success;
                OnUpdatedCallback?.Invoke(_updateHandles);
                AsyncStateCallback?.Invoke();
            }
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
            
            for (int i = 0; i < _updateHandles.Count; i++)
            {
                UpdateHandle updateHandle = _updateHandles[i];
                updateHandle.Dispose();
            }
            CommonHandleReleaseLogic();
            OperationSystem.RemoveOperation(this);
        }

        protected override bool CanBeReference()
        {
            return false;
        }
        
        public event Action<List<UpdateHandle>> OnLoaded
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
        
        public void AddUpdateHandle(UpdateHandle updateHandle)
        {
            _updateHandles.Add(updateHandle);
        }

        public void RemoveUpdateHandle(UpdateHandle updateHandle)
        {
            _updateHandles.Remove(updateHandle);
        }
        
        public HandleAwait<UpdateHandles> GetAwaiter()
        {
            return new HandleAwait<UpdateHandles>(this);
        }
    }
}
