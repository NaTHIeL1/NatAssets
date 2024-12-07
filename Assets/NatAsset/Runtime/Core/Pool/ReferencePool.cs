using System;
using System.Collections.Generic;
using System.Linq;

namespace NATFrameWork.NatAsset.Runtime
{
    //公共池管理器（句柄、加载任务、卸载任务等）
    internal static partial class ReferencePool
    {
        private static float _threshold = 0.5f;
        private static Dictionary<Type, Pool> _referenceDic = new Dictionary<Type, Pool>();

        internal static T Get<T>() where T : IReference, new()
        {
            Pool pool = GetOrCreatePool<T>();
            return pool.Get<T>();
        }

        internal static void Release(IReference reference)
        {
            if (_referenceDic == null || !_referenceDic.ContainsKey(reference.GetType())) 
            { 
                reference.Clear();
                return;
            }
            Pool pool = _referenceDic[reference.GetType()];
            pool.Release(reference);
        }

        internal static void ReleaseMemory()
        {
            for (int i = 0; i < _referenceDic.Count; i++)
            {
                KeyValuePair<Type, Pool> pool = _referenceDic.ElementAt(i);
                pool.Value.ReleaseMermory(_threshold);
            }
        }

        internal static void ReleaseAll()
        {
            _referenceDic.Clear();
        }

        private static Pool GetOrCreatePool<T>()
        {
            Type type = typeof(T);
            if (!_referenceDic.TryGetValue(type, out var pool))
            {
                pool = new Pool();
                _referenceDic.Add(type, pool);
            }

            return pool;
        }
    }

    internal static partial class ReferencePool
    {
        internal class Pool
        {
            private readonly List<IReference> list = new List<IReference>();

            internal T Get<T>() where T : IReference, new()
            {
                if (list.Count == 0)
                {
                    T obj = new T();
                    return obj;
                }

                IReference reference = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);

                return (T)reference;
            }

            internal void Release(IReference reference)
            {
                reference.Clear();
                list.Add(reference);
            }

            internal void ReleaseMermory(float threshold)
            {
                int rCount = (int)(list.Count * threshold);
                for (int i = 0; i < rCount; i++)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }
        }
    }
}