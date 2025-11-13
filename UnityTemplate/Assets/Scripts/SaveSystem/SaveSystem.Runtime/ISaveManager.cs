using System.Threading.Tasks;
using AsyncReactAwait.Bindable;
using SaveSystem.CustomSerialization;
using SaveSystem.Data;

namespace SaveSystem
{
    public interface ISaveManager
    {

        string CurrentSaveId { get; }
        bool SaveOnChangesEnabled { get; set; }
        int SaveOnChangesDebounceMs { get; set; }
        int MaxSaveOnChangesTimeMs { get; set; }
        
        IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged;

        T DeserializeAndCaptureSavableObject<T>(string valueKey, System.Func<T> factoryMethod = null, bool isMetaValue = false) where T : ISaveObject, new();

        IMutable<T> DeserializeAndCaptureCustomValue<T>(
            string valueKey, 
            System.Func<T> defaultValueFactory = null, 
            bool isMetaValue = false);

        Task SaveExplicitly();

        Task LoadOrCreate(string saveId);

        Task<IDataContainer> GetMetaData(string saveId);

        string[] GetSaves();

    }
}