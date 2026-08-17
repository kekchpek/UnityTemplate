using BigInteger = System.Numerics.BigInteger;
using AsyncReactAwait.Bindable;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleLargeResource
{
    public interface ISingleLargeResourceViewModel : IViewModel
    {
        IBindable<Sprite> Icon { get; }
        IBindable<BigInteger> Value { get; }
        void SetResource(string resourceId);
        void EnableIconLoading();
    }
}
