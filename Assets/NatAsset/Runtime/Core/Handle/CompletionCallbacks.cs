using System;
using UnityEngine;

namespace NATFrameWork.NatAsset.Runtime
{
    // A subscriber must not prevent other subscribers or the resource scheduler from finishing.
    internal static class CompletionCallbacks
    {
        internal static void Invoke(Action callbacks, string context)
        {
            if (callbacks == null) return;
            foreach (Action callback in callbacks.GetInvocationList())
                Run(callback, context);
        }

        internal static void Invoke<T>(Action<T> callbacks, T value, string context)
        {
            if (callbacks == null) return;
            foreach (Action<T> callback in callbacks.GetInvocationList())
                Run(() => callback(value), context);
        }

        internal static void Run(Action callback, string context)
        {
            try { callback(); }
            catch (Exception error)
            {
                Debug.LogError($"[NatAsset] 完成回调异常：{context}\n{error}");
            }
        }
    }
}
