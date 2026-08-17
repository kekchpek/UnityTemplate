using GameResources.Domain;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleLargeResource
{
    public interface ISingleLargeResourceViewHandle : IViewModel
    {
        void SetResource(ResourceId resourceId);
    }
}
