using System.Collections.Generic;
using GameResources.Domain;

namespace kekchpek.MVVM.Models.GameResources.Prices
{
    public interface ITextPrice : IEnumerable<(ResourceId resourceId, string amount)>
    {
    }
}
