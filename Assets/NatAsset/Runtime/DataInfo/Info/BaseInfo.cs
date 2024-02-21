namespace NATFrameWork.NatAsset.Runtime
{
    public abstract class BaseInfo : IReference
    {
        internal string InfoNameGUID { get; private set; } = string.Empty;
        internal int RefCount { get; private set; } = 0;
        internal bool isLocked { get; private set; } = false;
        protected BaseTask _unLoadTask = null;

        internal static T Create<T>(string infoName) where T : BaseInfo, new()
        {
            T info = ReferencePool.Get<T>();
            info.InfoNameGUID = infoName;
            info.OnCreate();
            return info;
        }

        internal static void Release(BaseInfo baseInfo)
        {
            ReferencePool.Release(baseInfo);
        }

        internal void AddRefCount()
        {
            OnAddRefCount();
            RefCount++;
        }

        internal void RedRefCount()
        {
            RefCount--;
            OnRedRefCount();
        }

        internal void Lock()
        {
            isLocked = true;
            OnLock();
        }

        internal void UnLock()
        {
            isLocked = false;
            OnUnLock();
        }

        //用于在启动强制卸载资源时启用，修改全部的引用计数为1
        internal void CollectionMark()
        {
            isLocked = false;
            if (RefCount <= 0) return;
            RefCount = 1;
        }

        protected virtual void OnCreate()
        {
        }

        protected virtual void OnLock()
        {
        }

        protected virtual void OnUnLock()
        {
        }

        protected virtual void OnCollectionMar()
        {
        }

        protected abstract void OnAddRefCount();
        protected abstract void OnRedRefCount();
        protected abstract void OnClear();

        public void Clear()
        {
            OnClear();
            RefCount = 0;
            InfoNameGUID = string.Empty;
            _unLoadTask = null;
        }

        protected bool CheckNeedUnLoadInfo()
        {
            if (isLocked) return false;
            if (RefCount == 0 && !isLocked)
            {
                return true;
            }

            return false;
        }
    }
}