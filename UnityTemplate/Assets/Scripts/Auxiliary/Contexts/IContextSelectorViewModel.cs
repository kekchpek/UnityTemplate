using System;
using UnityMVVM.ViewModelCore;

namespace kekchpek.Auxiliary.Contexts
{
    public interface IContextSelectorViewModel<out T> : IViewModel
    {
        event Action<T> ContextSelected;
    }
}