using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace NATFrameWork.NatAsset.Editor
{
    internal static class Extensions
    {
        internal static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }
        
        internal static List<T> GetInheritTypeObjects<T>()
        {
            List<T> objList = new List<T>();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<T>();
            foreach (Type type in types)
            {
                T obj = (T) Activator.CreateInstance(type);
                objList.Add(obj);
            }

            return objList;
        }
    }
}
