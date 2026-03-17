using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPrice : IEnumerable<(ResourceId resourceId, float amount)>
    {
        IBindable<bool> Affordable { get; }
    }
}