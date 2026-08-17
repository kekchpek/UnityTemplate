using AsyncReactAwait.Bindable;
using UnityEngine;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public interface ISingleResourceViewModel : IViewModel
    {
        IBindable<Sprite> Icon { get; }

        /// <summary>
        /// Formatted resource value. Available for both regular and large resources.
        /// </summary>
        IBindable<string> Value { get; }

        /// <summary>
        /// Numeric resource value. Meaningful only when <see cref="HasRawValue"/> is true,
        /// which is the case for regular (float) resources. Large resources expose
        /// <see cref="Value"/> only, since they can exceed the float range.
        /// </summary>
        IBindable<float> RawValue { get; }

        /// <summary>
        /// Whether the bound resource exposes a numeric value through <see cref="RawValue"/>.
        /// </summary>
        bool HasRawValue { get; }

        void SetResource(string resourceId);
        void EnableIconLoading();
    }
}
