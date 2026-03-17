using GameResources.Domain;
using UnityMVVM.ViewModelCore;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public interface ISingleResourceViewHandle : IViewModel
    {
        void SetResource(ResourceId resourceId);
    }
}