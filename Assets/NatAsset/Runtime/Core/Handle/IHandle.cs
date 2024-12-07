using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    public enum HandleState
    {
        Waiting,
        Loading,
        Finish,
    }

    public enum HandleResult
    {
        Nono,
        Success,
        Faild,
    }

    public abstract class IHandle : IReference
    {
        public string Name { get; protected set; }
        internal string Error { get; set; }
        protected HandleState _handleState = HandleState.Waiting;
        protected HandleResult _handleResult = HandleResult.Nono;
        protected BaseProvider _baseProvider;
        public bool IsValid => _isValid;
        public bool IsSuccess => _handleResult == HandleResult.Success;
        public bool IsFaile => _handleResult == HandleResult.Faild;
        public bool IsLoading => _handleState == HandleState.Loading;
        public bool IsDone => _handleState == HandleState.Finish;

        private bool _isCancel = false;
        private bool _isValid = true;

        internal void SetToInValid()
        {
            _isValid = false;
            _handleState = HandleState.Finish;
        }

        //异步结束未结束方法一起执行
        internal Action AsyncStateCallback;

        //被取消后卸载完成后执行
        internal Action CancelStateCallback;


        internal void CreateHandle(string name)
        {
            this.Name = name;
            _isCancel = false;
            _isValid = true;
            _handleState = HandleState.Loading;
        }

        internal void SetProvider(BaseProvider baseProvider)
        {
            _baseProvider = baseProvider;
            OnSetProvider(baseProvider);
        }

        protected virtual void OnSetProvider(BaseProvider baseProvider)
        {

        }

        protected bool CheckTokenCancel()
        {
            if (_isCancel)
            {
                Debug.LogWarningFormat("Handle:{0}:被取消", Name);
                CancelStateCallback?.Invoke();
                return true;
            }

            return false;
        }

        protected virtual void CheckError()
        {
            if (IsFaile && !string.IsNullOrEmpty(Error))
                Debug.LogErrorFormat("Handle:{0} ;HandleError:{1} ;Error:{2} ;", Name, GetType().Name, Error);
        }

        protected virtual void Cancel()
        {
            _isCancel = true;
            Debug.LogWarning($"{GetType().Name};Name:{Name} 被取消");
        }

        /// <summary>
        /// 卸载句柄
        /// </summary>
        public void Dispose()
        {
            OnDispose();
        }

        protected abstract void OnDispose();

        protected virtual bool CanBeReference()
        {
            return true;
        }

        protected void CommonHandleReleaseLogic()
        {
            if (!IsValid) return;
            if (_baseProvider != null)
            {
                _baseProvider.RemHandle(this);
                _baseProvider = null;
            }

            if (CanBeReference())
            {
                Release();
            }
            else
            {
                Clear();
            }
        }

        private void Release()
        {
            ReferencePool.Release(this);
        }

        public void Clear()
        {
            OnClear();
            _isValid = false;
            _isCancel = false;
            _baseProvider = null;
            AsyncStateCallback = null;
            CancelStateCallback = null;
            Error = string.Empty;
            Name = string.Empty;
            _handleState = HandleState.Waiting;
        }

        protected abstract void OnClear();
    }
}