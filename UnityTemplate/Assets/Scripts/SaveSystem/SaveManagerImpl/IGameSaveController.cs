using System.Collections.Generic;
using System.Threading.Tasks;
using kekchpek.GameSaves.Data;
using Cysharp.Threading.Tasks;
using kekchpek.SaveSystem.Codec;

namespace kekchpek.GameSaves
{
    public interface IGameSaveController
    {
        string CurrentSaveId { get; }

        void RefreshSelectedProfile();

        /// <returns>Hash to identify save data integrity. Can be null if save version is not integrity-protected.</returns>
        byte[] SaveExplicitly();

        void ToggleAutosave(bool enabled, long autosaveIntervalMs);

        void LoadOrCreate(string saveId);
        
        string[] GetSaveIds();

        void RemoveSave(string saveId);

        ICustomCodec<T> GetCodec<T>();

        UniTask<IReadOnlyList<T>> GetSaves<T>() where T : BaseSaveData, new();
    }
}