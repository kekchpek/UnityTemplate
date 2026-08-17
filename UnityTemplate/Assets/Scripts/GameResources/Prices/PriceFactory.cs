using System;
using System.Collections.Generic;
using GameResources.Domain;
using kekchpek.MVVM.Models.GameResources.Container;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public class PriceFactory<T> : IPriceFactory<T>
    {
        private readonly IResourcesContainer<T> _resourcesContainer;

        public PriceFactory(IResourcesContainer<T> resourcesContainer)
        {
            _resourcesContainer = resourcesContainer;
        }

        public IPrice<T> CreatePriceHandle(IReadOnlyList<(ResourceId resId, T amount)> price)
        {
            return new Price<T>(_resourcesContainer, price);
        }

        public IPrice<T> CreatePriceHandle(ReadOnlySpan<(ResourceId resId, T amount)> price)
        {
            return new Price<T>(_resourcesContainer, price.ToArray());
        }
    }
}
