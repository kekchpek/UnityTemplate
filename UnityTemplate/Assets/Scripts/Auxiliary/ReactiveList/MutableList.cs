using System.Collections;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Auxiliary.ReactiveList
{
    public class MutableList<T> : IMutableList<T>
    {

        private readonly List<T> _list;
        private readonly Mutable<T> _lastAdded = new();
        private readonly Mutable<T> _lastRemoved = new();

        public IBindable<T> LastAdded => _lastAdded;
        public IBindable<T> LastRemoved => _lastRemoved;
        public bool IsReadOnly => false;

        public MutableList()
        {
            _list = new List<T>();
        }

        public IEnumerator<T> GetEnumerator() =>
            _list.GetEnumerator();

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
            => _list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => _list.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (!_list.Remove(item))
                return false;
            _lastRemoved.ForceSet(item);
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

        public void RemoveAt(int index)
        {
            var outcome = _list[index];
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