using System.Threading.Tasks;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;

namespace kekchpek.SaveSystem
{
    public interface ISaveManager
    {

        string CurrentSaveId { get; }
        bool SaveOnChangesEnabled { get; set; }
        int SaveOnChangesDebounceMs { get; set; }
        int MaxSaveOnChangesTimeMs { get; set; }

        void SaveExplicitly();

        void LoadOrCreate(string saveId);

        Task<IDataContainer> GetMetaData(string saveId);

        string[] GetSaves();

    }
}