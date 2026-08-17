using BigInteger = System.Numerics.BigInteger;
using AsyncReactAwait.Bindable;
using GameResources.Domain;
using kekchpek.GameSaves;
using kekchpek.MVVM.Models.GameResources.Container;

namespace kekchpek.MVVM.Models.GameResources
{
    public class LargeResourcesModel : ResourcesContainer<BigInteger>, ILargeResourcesMutableModel
    {
        private readonly IGameSaveManager _gameSaveManager;

        public LargeResourcesModel(IGameSaveManager gameSaveManager)
        {
            _gameSaveManager = gameSaveManager;
        }

        protected override IMutable<BigInteger> CreateResourceValue(ResourceId id)
        {
            return _gameSaveManager.GameDataProvider.DeserializeAndCaptureCustomValue(
                $"LargeGameResource/{id}",
                () => BigInteger.Zero);
        }
    }
}
