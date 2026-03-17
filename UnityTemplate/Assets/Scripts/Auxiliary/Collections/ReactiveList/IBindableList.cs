using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Auxiliary.ReactiveList
{
    public interface IBindableList<T> : IReadOnlyList<T>
    {
        IBindable<T> LastAdded { get; }
        IBindable<T> LastRemoved { get; }
        ReadOnlySpan<T> GetReadOnlySpan();
        bool Contains(T item, IEqualityComparer<T> comparer = null);
    }
}