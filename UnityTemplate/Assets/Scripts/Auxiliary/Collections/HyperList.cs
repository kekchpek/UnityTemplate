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

        public ref T GetRef(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _items[index];
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
                Grow();
            _items[_count] = item;
            _count++;
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            while (items.Length + _count > _items.Length)
                Grow();
            items.CopyTo(_items.AsSpan(_count, items.Length));
            _count += items.Length;
        }

        public void Clear()
        {
            if (_count > 0)
            {
                _count = 0;
                Array.Clear(_items, 0, _items.Length);
            }
        }

        /// <summary>
        /// This could be used for lists of unmanaged structs to speed up clearing.
        /// </summary>
        public void ClearWithoutReleasingGcReferences()
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

        public void SwapAndRemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _count--;
            if (index < _count)
                (_items[index], _items[_count]) = (_items[_count], _items[index]);
            _items[_count] = default;
        }

        public void SwapAndRemoveAtWithoutReleaseGc(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            _count--;
            if (index < _count)
                (_items[index], _items[_count]) = (_items[_count], _items[index]);
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool SwapAndRemove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                SwapAndRemoveAt(index);
                return true;
            }
            return false;
        }

        public void InsertRange(int index, ReadOnlySpan<T> items)
        {
            if ((uint)index > (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (items.Length + _count > _items.Length)
                Grow((items.Length + _count) << 1);
            Array.Copy(_items, index, _items, index + items.Length, _count - index);
            items.CopyTo(_items.AsSpan(index, items.Length));
            _count += items.Length;
        }

        public void RemoveRange(int index, int count)
        {
            if ((uint)index + (uint)count > (uint)_count)
            {
                TrimEnd(index);
                return;
            }
            int tailLength = _count - index - count;
            Array.Copy(_items, index + count, _items, index, tailLength);
            Array.Clear(_items, _count - count, count);
            _count -= count;
        }

        public void RemoveRangeWithoutReleasingGcReferences(int index, int count)
        {
            if ((uint)index + (uint)count > (uint)_count)
            {
                TrimEndWithoutReleasingGcReferences(index);
                return;
            }
            int tailLength = _count - index - count;
            Array.Copy(_items, index + count, _items, index, tailLength);
            _count -= count;
        }

        public T PopBack()
        {
            if (_count == 0)
                throw new InvalidOperationException("List is empty");
            _count--;
            return _items[_count];
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

        public void TakeCopyFrom(ReadOnlySpan<T> source)
        {
            if (source.Length > _items.Length) {
                _items = new T[source.Length];
            }
            source.CopyTo(_items.AsSpan(0, source.Length));
            _count = source.Length;
        }

        public void CopyTo(Span<T> destination)
        {
            GetReadOnlySpan().CopyTo(destination);
        }

        public void TrimEndWithoutReleasingGcReferences(int clearFrom)
        {
            if (_count > clearFrom)
                _count = clearFrom;
        }

        public void TrimEnd(int clearFrom)
        {
            if (_count > clearFrom)
            {
                int oldCount = _count;
                _count = clearFrom;
                Array.Clear(_items, clearFrom, oldCount - clearFrom);
            }
        }

        public bool ContentEquals(ReadOnlySpan<T> other, IEqualityComparer<T> comparer = null)
        {
            if (other.Length != _count)
                return false;
            for (int i = 0; i < _count; i++)
            {
                if (!(comparer ?? EqualityComparer<T>.Default).Equals(_items[i], other[i]))
                    return false;
            }
            return true;
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

        private void Grow(int requiredCapacity)
        {
            if (requiredCapacity > _items.Length)
            {
                int newCapacity = requiredCapacity << 1;
                var newItems = new T[newCapacity];
                Array.Copy(_items, newItems, _count);
                _items = newItems;
            }
        }
    }
}
