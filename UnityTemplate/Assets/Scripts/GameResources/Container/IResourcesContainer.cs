using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Container
{
    public interface IResourcesContainer<T>
    {
        event Action<ResourceId, T> ResourceChanged;
        bool HasResource(ResourceId resourceId);
        IEnumerable<ResourceId> GetKnownResources();
        IEnumerable<ResourceId> GetNonZeroResources();
        IBindable<T> GetResource(ResourceId resourceId);
    }
}