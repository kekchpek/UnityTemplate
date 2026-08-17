using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPrice
    {
        IBindable<bool> Affordable { get; }

        ITextPrice GetTextRepresentation();
    }

    public interface IPrice<T> : IPrice, IEnumerable<(ResourceId resourceId, T amount)>
    {
        /// <summary>
        /// Allocation-free view over the price entries, for hot paths that would
        /// otherwise box the enumerator.
        /// </summary>
        ReadOnlySpan<(ResourceId resId, T amount)> Resources { get; }
    }
}
