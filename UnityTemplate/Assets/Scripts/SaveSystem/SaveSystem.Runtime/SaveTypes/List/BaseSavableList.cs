using System;
using System.Collections;
using System.Collections.Generic;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public abstract class BaseSavableList<T> : IList<T>, IReadOnlyList<T>, ISaveObject
    {

        public event Action Changed;

        private readonly List<T> _data = new();

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public List<T>.Enumerator GetEnumerator() => _data.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();

        public void Add(T item)
        {
            _data.Add(item);
            Changed?.Invoke();
        }

        public void Clear()
        {
            _data.Clear();
            Changed?.Invoke();
        }

        public bool Contains(T item) => _data.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _data.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            var outcome = _data.Remove(item);
            if (outcome)
            {
                Changed?.Invoke();
            }

            return outcome;
        }

        public int IndexOf(T item) => _data.IndexOf(item);

        public void Insert(int index, T item)
        {
            _data.Insert(index, item);
            Changed?.Invoke();
        }

        public void RemoveAt(int index)
        {
            _data.RemoveAt(index);
            Changed?.Invoke();
        }

        public T this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public void Deserialize(ILoadStream loadStream)
        {
            _data.Clear();
            var count = loadStream.LoadStruct<int>();
            if (_data.Capacity < count)
            {
                _data.Capacity = count;
            }
            for (var i = 0; i < count; i++)
            {
                _data.Add(DeserializeInternal(loadStream));
            }
        }

        protected abstract T DeserializeInternal(ILoadStream loadStream);

        public void Serialize(ISaveStream saveStream)
        {
            saveStream.SaveStruct(_data.Count);
            foreach (var element in _data)
            {
                SerializeInternal(saveStream, element);
            }
        }
        
        protected abstract void SerializeInternal(ISaveStream saveStream, T element);

    }
}