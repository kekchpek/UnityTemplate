using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public interface IMutableResourcesContainer : IResourcesContainer
    {
        void SetResource(ResourceId resourceId, float value);
    }
}