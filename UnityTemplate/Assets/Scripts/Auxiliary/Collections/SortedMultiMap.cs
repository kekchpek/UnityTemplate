using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kekchpek.Auxiliary.Collections
{
    public class SortedMultiMap<TKey, TValue> : IDictionary<TKey, TValue>
    {

        private readonly SortedDictionary<TKey, LinkedList<TValue>> _map;

        public SortedMultiMap() : this(null) { }
        public SortedMultiMap(IComparer<TKey> comparer = null)
        {
            _map = new SortedDictionary<TKey, LinkedList<TValue>>(comparer);
        }

        // Add a single value
        public void Add(TKey key, TValue value)
        {
            if (!_map.TryGetValue(key, out var list))
            {
                list = new LinkedList<TValue>();
                _map[key] = list;
            }
            list.AddLast(value); // preserves insertion order within the same key
        }

        // Add many values for the same key
        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (!_map.TryGetValue(key, out var list))
            {
                list = new LinkedList<TValue>();
                _map[key] = list;
            }
            foreach (var v in values) list.AddLast(v);
        }

        public bool TryGetValues(TKey key, out IEnumerable<TValue> values)
        {
            if (_map.TryGetValue(key, out var list))
            {
                values = list;
                return true;
            }
            values = Array.Empty<TValue>();
            return false;
        }

        public bool Remove(TKey key, TValue value)
        {
            if (_map.TryGetValue(key, out var list))
            {
                var node = list.Find(value);
                if (node != null)
                {
                    list.Remove(node);
                    if (list.Count == 0) _map.Remove(key);
                    return true;
                }
            }
            return false;
        }

        public bool RemoveKey(TKey key) => _map.Remove(key);

        public IEnumerable<KeyValuePair<TKey, TValue>> AsPairs()
        {
            // Yields (key, value) in sorted key order; duplicates come in insertion order
            foreach (var (key, list) in _map)
                foreach (var value in list)
                    yield return new KeyValuePair<TKey, TValue>(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _map.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return _map.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_map.TryGetValue(key, out var list))
            {
                value = list.First.Value;
                return true;
            }
            value = default;
            return false;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _map.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (_map.TryGetValue(item.Key, out var list))
            {
                return list.Contains(item.Value);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var (key, list) in _map)
            {
                foreach (var value in list)
                {
                    array[arrayIndex++] = new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key, item.Value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var (key, list) in _map)
            {
                foreach (var value in list)
                {
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<TKey> Keys => _map.Keys;
        public int CountKeys => _map.Count;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => _map.Keys;

        public ICollection<TValue> Values => _map.Values.SelectMany(list => list).ToList();

        public int Count => _map.Count;

        public bool IsReadOnly => false;

        public TValue this[TKey key] { get => _map[key].First.Value; set => _map[key].First.Value = value; }
    }
}