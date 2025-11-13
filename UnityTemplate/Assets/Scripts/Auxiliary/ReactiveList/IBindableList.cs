using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Auxiliary.ReactiveList
{
    public interface IBindableList<out T> : IReadOnlyList<T>
    {
        IBindable<T> LastAdded { get; }
        IBindable<T> LastRemoved { get; }
    }
}