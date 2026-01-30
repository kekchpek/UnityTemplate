using UnityMVVM.ViewModelCore;

namespace kekchpek.Auxiliary.Contexts
{
    public interface IContextedViewModel<in T> : IViewModel
    {
        void SetContext(T context);
    }
}