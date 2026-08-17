using System.Collections;
using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public class TextPrice : ITextPrice
    {
        private readonly IReadOnlyList<(ResourceId resourceId, string amount)> _resources;

        public TextPrice(IReadOnlyList<(ResourceId resourceId, string amount)> resources)
        {
            _resources = resources;
        }

        public IEnumerator<(ResourceId resourceId, string amount)> GetEnumerator()
        {
            return _resources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
