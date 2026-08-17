using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public interface IMutableResourcesContainer<T> : IResourcesContainer<T>
    {
        void RegisterResource(ResourceId resourceId);
        void SetResource(ResourceId resourceId, T value);
    }
}