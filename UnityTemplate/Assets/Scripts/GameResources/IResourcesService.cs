using System;
using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources
{
    public interface IResourcesService<T>
    {
        /// <summary>
        /// Raised after a resource amount is reduced, with the amount that was spent.
        /// </summary>
        event Action<ResourceId, T> ResourceSpent;

        /// <summary>
        /// Raised after a resource amount is increased, with the amount that was added.
        /// </summary>
        event Action<ResourceId, T> ResourceAdded;

        void Initialize();
        bool TryToSpend(IEnumerable<(ResourceId resourceId, T amount)> resources);

        /// <summary>
        /// Allocation-free overload of <see cref="TryToSpend(IEnumerable{ValueTuple{ResourceId, T}})"/>.
        /// </summary>
        bool TryToSpend(ReadOnlySpan<(ResourceId resourceId, T amount)> resources);

        bool TryToSpend(ResourceId resourceId, T amount);
        void Reduce(ResourceId resourceId, T amount);
        void Add(ResourceId resourceId, T amount);
    }
}
