using System;
using System.Runtime.CompilerServices;

namespace NATFrameWork.NatAsset.Runtime
{
    public struct HandleAwait<T> : INotifyCompletion where T:IHandle
    {
        public readonly T handle;

        public HandleAwait(T handle)
        {
            this.handle = handle;
        }

        public bool IsCompleted => handle?.IsDone ?? false;

        public T GetResult()
        {
            return handle;
        }

        public void OnCompleted(Action continuation)
        {
            if (handle != null)
            {
                handle.AsyncStateCallback += continuation;
            }
        }
    }
}