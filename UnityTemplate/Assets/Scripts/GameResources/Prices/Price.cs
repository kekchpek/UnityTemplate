using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kekchpek.MVVM.Models.GameResources.Container;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public class Price : IPrice
    {
        private readonly IReadOnlyList<(ResourceId resId, float amount)> _resourcesList;

        public IBindable<bool> Affordable { get; }

        public Price(
            IResourcesContainer resourcesContainer, 
            IReadOnlyList<(ResourceId resId, float amount)> resources)
        {
            var resourcesContainer1 = resourcesContainer;
            _resourcesList = resources;
            Affordable = Bindable.Aggregate(
                _resourcesList.Select(x => resourcesContainer1.GetResource(x.resId)),
                values =>
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        var val = values[i];
                        if (val < _resourcesList[i].amount)
                        {
                            return false;
                        }
                    }

                    return true;
                });
        }

        public IEnumerator<(ResourceId resourceId, float amount)> GetEnumerator()
        {
            return _resourcesList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}