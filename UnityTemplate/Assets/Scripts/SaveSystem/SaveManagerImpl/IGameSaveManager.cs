using AsyncReactAwait.Bindable;
using Cysharp.Threading.Tasks;
using kekchpek.SaveSystem;
using kekchpek.SaveSystem.Codec;

namespace kekchpek.GameSaves
{
    public interface IGameSaveManager
    {
        UniTask Initialize();
        IBindable<bool> IsInitialized { get; }
        ISaveDataProvider GameDataProvider { get; }
        ISaveDataProvider SettingsDataProvider { get; }
        void RegisterCustomCodec<T>(ICustomCodec<T> codec);
        ISaveDataProvider GetExclusiveDataProvider(string dataName);
    }
}