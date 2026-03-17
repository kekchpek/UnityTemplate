using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public interface IResourcesContainer
    {
        event Action<ResourceId, float> ResourceChanged;
        IEnumerable<ResourceId> GetKnownResources();
        IEnumerable<ResourceId> GetNonZeroResources();
        IBindable<float> GetResource(ResourceId resourceId);
    }
}