using AsyncReactAwait.Bindable;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public interface ISingleResourceViewModel : IViewModel
    {
        IBindable<Sprite> Icon { get; }
        IBindable<float> Value { get; }
        void SetResource(string resourceId);
        void EnableIconLoading();
    }
}