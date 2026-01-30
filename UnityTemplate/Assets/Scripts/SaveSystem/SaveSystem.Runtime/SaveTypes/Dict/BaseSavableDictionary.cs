using System;
using System.Collections;
using System.Collections.Generic;
using kekchpek.SaveSystem.CustomSerialization;
using UnityEngine;

namespace kekchpek.SaveSystem.SaveTypes
{
    public abstract class BaseSavableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISaveObject
    {

        private readonly Dictionary<TKey, TValue> _dict = new();

        public TValue this[TKey key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<TKey> Keys => _dict.Keys;

        public ICollection<TValue> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public event Action Changed;

        public void Add(TKey key, TValue value)
        {
            _dict.Add(key, value);
            Changed?.Invoke();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Add(item);
            Changed?.Invoke();
        }

        public void Clear()
        {
            _dict.Clear();
            Changed?.Invoke();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(TKey key)
        {
            var result = _dict.Remove(key);
            if (result)
                Changed?.Invoke();
            return result;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = ((ICollection<KeyValuePair<TKey, TValue>>)_dict).Remove(item);
            if (result)
                Changed?.Invoke();
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Deserialize(ILoadStream loadStream)
        {
            if (_dict.Count > 0) {
                Debug.LogError("Dictionary is not empty");
            }
            _dict.Clear();
            var count = loadStream.LoadStruct<int>();
            for (var i = 0; i < count; i++)
            {
                var key = DeserializeKeyInternal(loadStream);
                var value = DeserializeValueInternal(loadStream);
                _dict.Add(key, value);
            }
        }

        protected abstract TKey DeserializeKeyInternal(ILoadStream loadStream);
        protected abstract TValue DeserializeValueInternal(ILoadStream loadStream);

        public void Serialize(ISaveStream saveStream)
        {
            saveStream.SaveStruct(_dict.Count);
            foreach (var kvp in _dict)
            {
                SerializeKeyInternal(saveStream, kvp.Key);
                SerializeValueInternal(saveStream, kvp.Value);
            }
        }
        
        protected abstract void SerializeKeyInternal(ISaveStream saveStream, TKey key);
        protected abstract void SerializeValueInternal(ISaveStream saveStream, TValue value);
    }
}