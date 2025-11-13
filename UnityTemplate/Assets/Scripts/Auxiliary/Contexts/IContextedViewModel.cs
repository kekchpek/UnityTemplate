using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Views.Infrastructure.Contexts
{
    public interface IContextedViewModel<in T> : IViewModel
    {
        void SetContext(T context);
    }
}