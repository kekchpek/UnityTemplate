using BasicSettings.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BasicSettings
{
    public interface ISettingsService
    {
        UniTask Initialize();
        
        void SetEnableAudioFX(bool enable);
        void SetEnableAudioMusic(bool enable);
        void SetEnableAudioMaster(bool enable);
        void SetEnableAudioDialogue(bool enable);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetEffectsVolume(float volume);
        void SetDialogueVolume(float volume);
        void SetResolution(Vector2Int resolution);
        void SetAspectRatio(float aspectRatio);
        void SetDisplayMode(DisplayMode displayMode);
    }
}