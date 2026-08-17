using System.Threading.Tasks;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;

namespace kekchpek.SaveSystem
{
    public interface ISaveManager
    {

        string CurrentSaveId { get; }

        /// <summary>
        /// When enabled, changes reported by captured save objects schedule a debounced save.
        /// </summary>
        bool SaveOnChangesEnabled { get; set; }

        /// <summary>
        /// How long to wait after the last change before writing the save.
        /// </summary>
        int SaveOnChangesDebounceMs { get; set; }

        /// <summary>
        /// Upper bound on how long changes may keep deferring a save.
        /// </summary>
        int MaxSaveOnChangesTimeMs { get; set; }

        void UseExplicitSaveSystemVersion(int version);

        void RegisterCustomCodec<T>(ICustomCodec<T> codec);

        /// <returns>Hash to identify save data integrity. Can be null if save version is not integrity-protected.</returns>
        byte[] SaveExplicitly();

        void LoadOrCreate(string saveId);

        Task<IDataContainer> GetMetaData(string saveId);

        string[] GetSaves();

        void RemoveSave(string saveId);

        /// <returns>Integrity hash for the save, or null if missing, invalid, or not integrity-protected.</returns>
        byte[] GetSaveHash(string saveId);

    }
}
