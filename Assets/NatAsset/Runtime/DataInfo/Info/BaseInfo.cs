namespace NATFrameWork.NatAsset.Runtime
{
    public abstract class BaseInfo : IReference
    {
        private static long _nextGeneration;
        internal long Generation { get; private set; }
        internal string InfoNameGUID { get; private set; } = string.Empty;
        internal int RefCount { get; private set; } = 0;
        internal bool isLocked { get; private set; } = false;
        protected BaseTask _unLoadTask = null;

        internal static T Create<T>(string infoName) where T : BaseInfo, new()
        {
            T info = ReferencePool.Get<T>();
            info.InfoNameGUID = infoName;
            info.Generation = ++_nextGeneration;
            info.OnCreate();
            return info;
        }

        internal static void Release(BaseInfo baseInfo)
        {
            ReferencePool.Release(baseInfo);
        }

        internal void AddRefCount()
        {
            RefCount++;
            OnAddRefCount();
        }

        internal void RedRefCount()
        {
            if (RefCount == 0)
                throw new System.InvalidOperationException("资源引用重复释放: " + InfoNameGUID);
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

        protected virtual void OnCreate()
        {
        }

        protected virtual void OnLock()
        {
        }

        protected virtual void OnUnLock()
        {
        }

        protected abstract void OnAddRefCount();
        protected abstract void OnRedRefCount();
        protected abstract void OnClear();

        public void Clear()
        {
            OnClear();
            Generation = 0;
            RefCount = 0;
            isLocked = false;
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
