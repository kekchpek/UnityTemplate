using System.Collections.Generic;
using System.Threading.Tasks;
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
        public string CurrentSaveId => "no_save";

        public ISaveDataProvider GameDataProvider => new EmptySaveDataProvider();

        public ISaveDataProvider SettingsDataProvider => new EmptySaveDataProvider();

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

        public IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged
        {
            return new Mutable<T>(defaultValue);
        }

        public T DeserializeAndCaptureSavableObject<T>(string valueKey, System.Func<T> factoryMethod = null, bool isMetaValue = false) where T : ISaveObject, new()
        {
            if (factoryMethod != null)
            {
                return factoryMethod();
            }
            return new T();
        }

        public IMutable<T> DeserializeAndCaptureCustomValue<T>(string valueKey, System.Func<T> defaultValueFactory = null, bool isMetaValue = false)
        {
            if (defaultValueFactory != null)
            {
                return new Mutable<T>(defaultValueFactory());
            }
            return new Mutable<T>(default);
        }

        public ISaveDataProvider GetExclusiveDataProvider(string dataName)
        {
            return new EmptySaveDataProvider();
        }

        public void RefreshSelectedProfile()
        {
            // Do nothing
        }

        public void SaveExplicitly()
        {
            // Do nothing
        }

        public void LoadOrCreate(string saveId)
        {
            // Do nothing
        }

        public UniTask<IReadOnlyList<SaveData>> GetSaves()
        {
            return UniTask.FromResult<IReadOnlyList<SaveData>>(System.Array.Empty<SaveData>());
        }
    }
}