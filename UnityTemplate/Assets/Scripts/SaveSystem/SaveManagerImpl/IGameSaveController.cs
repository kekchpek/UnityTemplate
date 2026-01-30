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

        void SaveExplicitly();

        void LoadOrCreate(string saveId);
        
        string[] GetSaveIds();

        ICustomCodec<T> GetCodec<T>();

        UniTask<IReadOnlyList<SaveData>> GetSaves();
    }
}