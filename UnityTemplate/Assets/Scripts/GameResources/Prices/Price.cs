using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigInteger = System.Numerics.BigInteger;
using kekchpek.MVVM.Models.GameResources.Container;
using kekchpek.MVVM.Models.GameResources.Static;
using AsyncReactAwait.Bindable;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public class Price<T> : IPrice<T>
    {
        private readonly (ResourceId resId, T amount)[] _resourcesList;
        private readonly ITextPrice _textRepresentation;

        public IBindable<bool> Affordable { get; }

        public ReadOnlySpan<(ResourceId resId, T amount)> Resources => _resourcesList;

        public Price(
            IResourcesContainer<T> resourcesContainer,
            IReadOnlyList<(ResourceId resId, T amount)> resources)
        {
            // Copied so the price is immutable regardless of what the caller does with the list.
            _resourcesList = new (ResourceId resId, T amount)[resources.Count];
            for (var i = 0; i < resources.Count; i++)
            {
                _resourcesList[i] = resources[i];
            }
            _textRepresentation = CreateTextRepresentation(_resourcesList);
            Affordable = Bindable.Aggregate(
                _resourcesList.Select(x => resourcesContainer.GetResource(x.resId)),
                values =>
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        if (Comparer<T>.Default.Compare(values[i], _resourcesList[i].amount) < 0)
                            return false;
                    }

                    return true;
                });
        }

        public ITextPrice GetTextRepresentation()
        {
            return _textRepresentation;
        }

        public IEnumerator<(ResourceId resourceId, T amount)> GetEnumerator()
        {
            return ((IEnumerable<(ResourceId resourceId, T amount)>)_resourcesList).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static ITextPrice CreateTextRepresentation(IReadOnlyList<(ResourceId resId, T amount)> resources)
        {
            var textResources = new (ResourceId resourceId, string amount)[resources.Count];
            for (var i = 0; i < resources.Count; i++)
            {
                var (resourceId, amount) = resources[i];
                textResources[i] = (resourceId, FormatAmount(amount));
            }

            return new TextPrice(textResources);
        }

        private static string FormatAmount(T amount)
        {
            return amount switch
            {
                float floatAmount => ResourceFormatting.FormatNumber(floatAmount),
                BigInteger bigIntegerAmount => ResourceFormatting.FormatNumber(bigIntegerAmount),
                _ => throw new NotSupportedException($"Price formatting is not supported for {typeof(T).Name}.")
            };
        }
    }
}
