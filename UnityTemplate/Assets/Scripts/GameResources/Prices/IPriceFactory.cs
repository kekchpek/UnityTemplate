using System;
using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface IPriceFactory<T>
    {
        IPrice<T> CreatePriceHandle(IReadOnlyList<(ResourceId resId, T amount)> price);

        /// <summary>
        /// Allocation-free overload for callers building a price on the stack.
        /// </summary>
        IPrice<T> CreatePriceHandle(ReadOnlySpan<(ResourceId resId, T amount)> price);
    }
}
