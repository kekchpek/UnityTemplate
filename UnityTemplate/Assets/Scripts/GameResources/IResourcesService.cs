using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources
{
    public interface IResourcesService
    {
        bool TryToSpend(IEnumerable<(ResourceId resourceId, float amount)> resources);
        bool TryToSpend(ResourceId resourceId, float amount);
        void Reduce(ResourceId resourceId, float amount);
        void Add(ResourceId resourceId, float amount);
    }
}