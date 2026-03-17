using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPriceBuilder
    {
        IPriceBuilder Add(ResourceId resId, float amount);
        IPrice Build();
    }
}