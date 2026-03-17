using System;
using System.Collections;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using kekchpek.Auxiliary.Collections;

namespace kekchpek.Auxiliary.ReactiveList
{
    public class MutableList<T> : IMutableList<T>
    {
        private readonly HyperList<T> _list;
        private readonly Mutable<T> _lastAdded = new();
        private readonly Mutable<T> _lastRemoved = new();

        public IBindable<T> LastAdded => _lastAdded;
        public IBindable<T> LastRemoved => _lastRemoved;
        public bool IsReadOnly => false;

        public MutableList()
        {
            _list = new HyperList<T>();
        }

        public MutableList(int capacity)
        {
            _list = new HyperList<T>(capacity);
        }

        public ReadOnlySpan<T> GetReadOnlySpan() => _list.GetReadOnlySpan();

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _list.Count; i++)
                yield return _list[i];
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public void Add(T item)
        {
            _list.Add(item);
            _lastAdded.ForceSet(item);
        }

        public void Clear()
        {
            while (Count > 0)
            {
                RemoveAt(Count - 1);
            }
        }

        public bool Contains(T item)
            => _list.IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
            => _list.GetReadOnlySpan().CopyTo(array.AsSpan(arrayIndex));

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0)
                return false;
            T outcome = _list[index];
            _list.RemoveAt(index);
            _lastRemoved.ForceSet(outcome);
            return true;
        }

        public int Count => _list.Count;

        public int IndexOf(T item)
            => _list.IndexOf(item);

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            _lastAdded.ForceSet(item);
        }
        
        public bool Contains(T item, IEqualityComparer<T> comparer = null)
            => _list.Contains(item, comparer);

        public void RemoveAt(int index)
        {
            T outcome = _list[index];
            _list.RemoveAt(index);
            _lastRemoved.ForceSet(outcome);
        }

        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
    }
}
