using System;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Views.Infrastructure.Contexts
{
    public interface IContextSelectorViewModel<out T> : IViewModel
    {
        event Action<T> ContextSelected;
    }
}