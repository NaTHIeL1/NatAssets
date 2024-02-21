using System.Collections.Generic;
using System.Collections.Specialized;

namespace NATFrameWork.NatAsset.Runtime
{
    public class ListDic<Key, Value>
    {
        private List<Value> _list;
        private Dictionary<Key, Value> _dictionary;
        
        public ListDic(int count = -1)
        {
            if (count == -1)
            {
                _list = new List<Value>();
                _dictionary = new Dictionary<Key, Value>();
            }
            else
            {
                _list = new List<Value>(count);
                _dictionary = new Dictionary<Key, Value>(count);
            }
        }

        public void Add(Key key, Value value)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _list.Add(value);
                _dictionary.Add(key, value);
            }
        }

        public void Remove(Key key)
        {
            if (_dictionary.TryGetValue(key, out Value value))
            {
                _dictionary.Remove(key);
                _list.Remove(value);
            }
        }

        public bool TryGetValue(Key key, out Value value)
        {
            if (_dictionary.TryGetValue(key, out value))
            {
                return true;
            }
            return false;
        }

        public bool ContainsKey(Key key)
        {
            return _dictionary.ContainsKey(key);
        }

        public Value GetElementByIndex(int index)
        {
            Value v = _list[index];
            return v;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
        }

        public void Sort()
        {
            _list.Sort();
        }
        public int Count => _list.Count;
    }
}
