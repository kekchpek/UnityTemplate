using GameResources.Domain;
using kekchpek.MVVM.Models.GameResources.Container;
using kekchpek.GameSaves;
using AsyncReactAwait.Bindable;

namespace kekchpek.MVVM.Models.GameResources
{
    public class ResourcesModel : ResourcesContainer, IResourcesMutableModel
    {
        
        private readonly IGameSaveManager _gameSaveManager;

        public ResourcesModel(IGameSaveManager gameSaveManager)
        {
            _gameSaveManager = gameSaveManager;
        }

        protected override IMutable<float> CreateResourceValue(ResourceId id)
        {
            return _gameSaveManager.GameDataProvider.DeserializeAndCaptureStructValue($"GameResource/{id}", 0f);
        }
    }
}