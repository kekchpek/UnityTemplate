using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPriceBuilder<T>
    {
        IPriceBuilder<T> Add(ResourceId resId, T amount);
        IPrice<T> Build();
    }
}
