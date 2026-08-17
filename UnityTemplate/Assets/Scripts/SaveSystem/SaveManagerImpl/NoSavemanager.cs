using System.Collections.Generic;
using kekchpek.GameSaves.Data;
using AsyncReactAwait.Bindable;
using Cysharp.Threading.Tasks;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem;
using kekchpek.GameSaves.Mock;

namespace kekchpek.GameSaves
{
    public class NoSaveManager : IGameSaveManager, IGameSaveController
    {
        private readonly IMutableFactory _mutableFactory;

        public NoSaveManager(IMutableFactory mutableFactory = null)
        {
            _mutableFactory = mutableFactory ?? DefaultMutableFactory.Instance;
        }

        public string CurrentSaveId => "no_save";

        public IMultifileSaveDataProvider GameDataProvider => new EmptySaveDataProvider(_mutableFactory);

        public ISaveDataProvider SettingsDataProvider => new EmptySaveDataProvider(_mutableFactory);

        public IBindable<bool> IsInitialized { get; } = new Mutable<bool>(true);

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public string[] GetSaveIds()
        {
            return System.Array.Empty<string>();
        }

        public ICustomCodec<T> GetCodec<T>()
        {
            return null;
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
        }

        public ISaveDataProvider GetExclusiveDataProvider(string dataName)
        {
            return new EmptySaveDataProvider(_mutableFactory);
        }

        public void RefreshSelectedProfile()
        {
            // Do nothing
        }

        public byte[] SaveExplicitly()
        {
            return null;
        }

        public void ToggleAutosave(bool enabled, long autosaveIntervalMs)
        {
            // Do nothing
        }

        public void LoadOrCreate(string saveId)
        {
            // Do nothing
        }

        public void RemoveSave(string saveId)
        {
            // Do nothing
        }

        public UniTask<IReadOnlyList<T>> GetSaves<T>() where T : BaseSaveData, new()
        {
            return UniTask.FromResult<IReadOnlyList<T>>(System.Array.Empty<T>());
        }

        public void UnregisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            // Do nothing
        }
    }
}
