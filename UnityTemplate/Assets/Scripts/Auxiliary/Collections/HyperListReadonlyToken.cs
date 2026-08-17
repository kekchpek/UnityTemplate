using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.Collections
{
    public readonly struct HyperListReadonlyToken<T>
    {
        private readonly HyperList<T> _list;

        public HyperListReadonlyToken(HyperList<T> list)
        {
            _list = list;
        }

        public int Count => _list?.Count ?? 0;

        public T this[int index]
        {
            get
            {
                if (_list == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                return _list[index];
            }
        }

        public int IndexOf(T item, IEqualityComparer<T> comparer = null)
        {
            return _list?.IndexOf(item, comparer) ?? -1;
        }

        public bool Contains(T item, IEqualityComparer<T> comparer = null)
        {
            return _list?.Contains(item, comparer) ?? false;
        }

        public void CopyTo(Span<T> destination)
        {
            if (_list == null)
            {
                return;
            }
            _list.CopyTo(destination);
        }

        public ReadOnlySpan<T> GetReadOnlySpan()
        {
            return _list == null ? ReadOnlySpan<T>.Empty : _list.GetReadOnlySpan();
        }

        public static implicit operator HyperListReadonlyToken<T>(HyperList<T> list)
        {
            return new HyperListReadonlyToken<T>(list);
        }
    }
}
