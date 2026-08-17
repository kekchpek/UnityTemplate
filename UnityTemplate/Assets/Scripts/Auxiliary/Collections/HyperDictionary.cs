using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.Collections
{
    public class HyperDictionary<TKey, TValue>
    {
        private struct Entry
        {
            public int HashCode;
            public TKey Key;
            public TValue Value;
        }

        private const int DefaultCapacity = 4;
        private const int EmptyHash = 0;

        private static readonly EqualityComparer<TKey> Comparer = EqualityComparer<TKey>.Default;

        private Entry[] _entries = Array.Empty<Entry>();
        private int _count;

        public int Count => _count;

        public HyperDictionary(int capacity = 1)
        {
            if (capacity > 0)
            {
                GrowTo(GetPowerOfTwo(capacity));
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (_entries.Length == 0 || _count * 2 >= _entries.Length)
            {
                GrowTo(_entries.Length == 0 ? DefaultCapacity : _entries.Length << 1);
            }

            Insert(key, value);
        }

        public TValue Get(TKey key)
        {
            int hashCode = GetHashCode(key);
            int index = hashCode % _entries.Length;

            while (true)
            {
                ref Entry entry = ref _entries[index];
                if (entry.HashCode == EmptyHash)
                {
                    return entry.Value;
                }

                if (entry.HashCode == hashCode && Comparer.Equals(entry.Key, key))
                {
                    return entry.Value;
                }

                index = (index + 1) % _entries.Length;
            }
        }

        public bool HasKey(TKey key)
        {
            int hashCode = GetHashCode(key);
            int index = hashCode % _entries.Length;

            while (true)
            {
                ref Entry entry = ref _entries[index];
                if (entry.HashCode == EmptyHash)
                {
                    return false;
                }

                if (entry.HashCode == hashCode && Comparer.Equals(entry.Key, key))
                {
                    return true;
                }

                index = (index + 1) % _entries.Length;
            }
        }

        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
            _count = 0;
        }

        private void Insert(TKey key, TValue value)
        {
            int hashCode = GetHashCode(key);
            int index = hashCode % _entries.Length;

            while (true)
            {
                ref Entry entry = ref _entries[index];
                if (entry.HashCode == EmptyHash)
                {
                    entry.HashCode = hashCode;
                    entry.Key = key;
                    entry.Value = value;
                    _count++;
                    return;
                }

                if (entry.HashCode == hashCode && Comparer.Equals(entry.Key, key))
                {
                    entry.Value = value;
                    return;
                }

                index = (index + 1) % _entries.Length;
            }
        }

        private static int GetHashCode(TKey key)
        {
            int hashCode = Comparer.GetHashCode(key);
            return hashCode == EmptyHash ? 1 : hashCode;
        }

        private void GrowTo(int capacity)
        {
            var newEntries = new Entry[capacity];
            var oldEntries = _entries;

            _entries = newEntries;
            _count = 0;

            for (int i = 0; i < oldEntries.Length; i++)
            {
                ref Entry entry = ref oldEntries[i];
                if (entry.HashCode != EmptyHash)
                {
                    Insert(entry.Key, entry.Value);
                }
            }
        }

        private static int GetPowerOfTwo(int value)
        {
            int capacity = DefaultCapacity;
            while (capacity < value)
            {
                capacity <<= 1;
            }

            return capacity;
        }
    }
}
