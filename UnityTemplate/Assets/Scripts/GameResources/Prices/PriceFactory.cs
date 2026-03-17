using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public class PriceFactory : IPriceFactory
    {
        private readonly IResourcesModel _resourcesModel;

        public PriceFactory(IResourcesModel resourcesModel)
        {
            _resourcesModel = resourcesModel;
        }
        
        public IPrice CreatePriceHandle(IReadOnlyList<(ResourceId resId, float amount)> price)
        {
            return new Price(_resourcesModel, price);
        }
    }
}