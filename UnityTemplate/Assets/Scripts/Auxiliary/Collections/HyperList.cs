using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.Collections
{
    public class HyperList<T>
    {
        private const int DefaultCapacity = 4;

        private T[] _items;
        private int _count;

        public int Count => _count;
        public T[] Buffer => _items;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                _items[index] = value;
            }
        }

        public HyperList(int capacity = 0)
        {
            _items = capacity > 0 ? new T[capacity] : Array.Empty<T>();
            _count = 0;
        }

        public HyperList(T[] initialBuffer) {
            _items = initialBuffer;
            _count = initialBuffer.Length;
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
                Grow();
            _items[_count] = item;
            _count++;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _count--;
            if (index < _count)
                Array.Copy(_items, index + 1, _items, index, _count - index);
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_count == _items.Length)
                Grow();
            if (index < _count)
                Array.Copy(_items, index, _items, index + 1, _count - index);
            _items[index] = item;
            _count++;
        }

        public int IndexOf(T item, IEqualityComparer<T> comparer = null)
        {
            for (int i = 0; i < _count; i++)
            {
                if ((comparer ?? EqualityComparer<T>.Default).Equals(_items[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool Contains(T item, IEqualityComparer<T> comparer = null)
        {
            var span = GetReadOnlySpan();
            for (int i = 0; i < _count; i++)
            {
                if ((comparer ?? EqualityComparer<T>.Default).Equals(_items[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void TakeCopy(ReadOnlySpan<T> source) 
        {
            if (source.Length > _items.Length) {
                _items = new T[source.Length];
            }
            source.CopyTo(_items.AsSpan(0, source.Length));
            _count = source.Length;
        }

        public Span<T> GetSpan()
        {
            return _items.AsSpan(0, _count);
        }
        

        public ReadOnlySpan<T> GetReadOnlySpan()
        {
            return _items.AsSpan(0, _count);
        }

        private void Grow()
        {
            int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
            var newItems = new T[newCapacity];
            Array.Copy(_items, newItems, _count);
            _items = newItems;
        }
    }
}
