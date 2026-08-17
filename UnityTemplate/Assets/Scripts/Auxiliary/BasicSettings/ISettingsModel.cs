using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using BasicSettings.Data;
using UnityEngine;

namespace BasicSettings
{
    public interface ISettingsModel
    {
        IBindable<float> MasterVolume { get; }
        IBindable<float> MusicVolume { get; }
        IBindable<float> EffectsVolume { get; }
        IBindable<float> DialogueVolume { get; }
        IBindable<bool> IsAudioMasterEnabled { get; }
        IBindable<bool> IsAudioFxEnabled { get; }
        IBindable<bool> IsAudioMusicEnabled { get; }
        IBindable<bool> IsAudioDialogueEnabled { get; }

        IBindable<Vector2Int> Resolution { get; }
        IBindable<float> AspectRatio { get; }
        IBindable<DisplayMode> DisplayMode { get; }
        IBindable<List<Vector2Int>> AvailableResolutions { get; }
        IBindable<List<DisplayMode>> AvailableDisplayModes { get; }

        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetEffectsVolume(float volume);
        void SetDialogueVolume(float volume);
        void SetAudioMasterEnabled(bool enabled);
        void SetAudioFxEnabled(bool enabled);
        void SetAudioMusicEnabled(bool enabled);
        void SetAudioDialogueEnabled(bool enabled);
        void SetResolution(Vector2Int resolution);
        void SetAspectRatio(float aspectRatio);
        void SetDisplayMode(DisplayMode displayMode);
        void SetAvailableResolutions(List<Vector2Int> resolutions);
        void SetAvailableDisplayModes(List<DisplayMode> displayModes);
    }
}
