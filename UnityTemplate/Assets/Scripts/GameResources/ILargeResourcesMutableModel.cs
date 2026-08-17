using BigInteger = System.Numerics.BigInteger;
using kekchpek.MVVM.Models.GameResources.Container;

namespace kekchpek.MVVM.Models.GameResources
{
    public interface ILargeResourcesMutableModel : IMutableResourcesContainer<BigInteger>, ILargeResourcesModel
    {
    }
}
