using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPriceFactory
    {
        IPrice CreatePriceHandle(IReadOnlyList<(ResourceId resId, float amount)> price);
    }
}