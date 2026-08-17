using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using kekchpek.GameSaves;
using BasicSettings.Data;
using UnityEngine;

namespace BasicSettings
{
    public class SettingsModel : ISettingsModel
    {
        private readonly IGameSaveManager _gameSaveManager;
        private readonly IMutable<List<Vector2Int>> _availableResolutions = new Mutable<List<Vector2Int>>(new List<Vector2Int>());
        private readonly IMutable<List<DisplayMode>> _availableDisplayModes = new Mutable<List<DisplayMode>>(new List<DisplayMode>());

        private IMutable<float> _masterVolume;
        private IMutable<float> _musicVolume;
        private IMutable<float> _effectsVolume;
        private IMutable<float> _dialogueVolume;
        private IMutable<bool> _isAudioMasterEnabled;
        private IMutable<bool> _isAudioFxEnabled;
        private IMutable<bool> _isAudioMusicEnabled;
        private IMutable<bool> _isAudioDialogueEnabled;

        private IMutable<Vector2Int> _resolution;
        private IMutable<float> _aspectRatio;
        private IMutable<DisplayMode> _displayMode;

        public IBindable<float> MasterVolume => _masterVolume;
        public IBindable<float> MusicVolume => _musicVolume;
        public IBindable<float> EffectsVolume => _effectsVolume;
        public IBindable<float> DialogueVolume => _dialogueVolume;
        public IBindable<bool> IsAudioMasterEnabled => _isAudioMasterEnabled;
        public IBindable<bool> IsAudioFxEnabled => _isAudioFxEnabled;
        public IBindable<bool> IsAudioMusicEnabled => _isAudioMusicEnabled;
        public IBindable<bool> IsAudioDialogueEnabled => _isAudioDialogueEnabled;

        public IBindable<Vector2Int> Resolution => _resolution;
        public IBindable<float> AspectRatio => _aspectRatio;
        public IBindable<DisplayMode> DisplayMode => _displayMode;
        public IBindable<List<Vector2Int>> AvailableResolutions => _availableResolutions;
        public IBindable<List<DisplayMode>> AvailableDisplayModes => _availableDisplayModes;

        public SettingsModel(IGameSaveManager gameSaveManager)
        {
            _gameSaveManager = gameSaveManager;
            _gameSaveManager.IsInitialized.Bind(OnGameSaveManagerInitialized);
        }

        private void OnGameSaveManagerInitialized(bool isInitialized)
        {
            if (isInitialized)
            {
                LoadAudioSettings();
                LoadDisplaySettings();
                _gameSaveManager.IsInitialized.Unbind(OnGameSaveManagerInitialized);
            }
        }

        private void LoadAudioSettings()
        {
            var provider = _gameSaveManager.SettingsDataProvider;
            _masterVolume = provider.DeserializeAndCaptureStructValue(SettingsKeys.MASTER_VOLUME_KEY, 50f);
            _musicVolume = provider.DeserializeAndCaptureStructValue(SettingsKeys.MUSIC_VOLUME_KEY, 50f);
            _effectsVolume = provider.DeserializeAndCaptureStructValue(SettingsKeys.EFFECTS_VOLUME_KEY, 50f);
            _dialogueVolume = provider.DeserializeAndCaptureStructValue(SettingsKeys.DIALOGUE_VOLUME_KEY, 50f);
            _isAudioMasterEnabled = provider.DeserializeAndCaptureStructValue(SettingsKeys.AUDIO_MASTER_KEY, true);
            _isAudioFxEnabled = provider.DeserializeAndCaptureStructValue(SettingsKeys.AUDIO_FX_KEY, true);
            _isAudioMusicEnabled = provider.DeserializeAndCaptureStructValue(SettingsKeys.AUDIO_MUSIC_KEY, true);
            _isAudioDialogueEnabled = provider.DeserializeAndCaptureStructValue(SettingsKeys.AUDIO_DIALOGUE_KEY, true);
        }

        private void LoadDisplaySettings()
        {
            var defaultResolution = new Vector2Int(Screen.resolutions[^1].width, Screen.resolutions[^1].height);
            var defaultAspectRatio = (float)defaultResolution.x / defaultResolution.y;

            var provider = _gameSaveManager.SettingsDataProvider;
            _resolution = provider.DeserializeAndCaptureStructValue(SettingsKeys.RESOLUTION_KEY, defaultResolution);
            _aspectRatio = provider.DeserializeAndCaptureStructValue(SettingsKeys.ASPECT_RATIO_KEY, defaultAspectRatio);
            _displayMode = provider.DeserializeAndCaptureStructValue(SettingsKeys.DISPLAY_MODE_KEY, Data.DisplayMode.Windowed);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume.Value = volume;
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume.Value = volume;
        }

        public void SetEffectsVolume(float volume)
        {
            _effectsVolume.Value = volume;
        }

        public void SetDialogueVolume(float volume)
        {
            _dialogueVolume.Value = volume;
        }

        public void SetAudioMasterEnabled(bool enabled)
        {
            _isAudioMasterEnabled.Value = enabled;
        }

        public void SetAudioFxEnabled(bool enabled)
        {
            _isAudioFxEnabled.Value = enabled;
        }

        public void SetAudioMusicEnabled(bool enabled)
        {
            _isAudioMusicEnabled.Value = enabled;
        }

        public void SetAudioDialogueEnabled(bool enabled)
        {
            _isAudioDialogueEnabled.Value = enabled;
        }

        public void SetResolution(Vector2Int resolution)
        {
            _resolution.Value = resolution;
            _aspectRatio.Value = (float)resolution.x / resolution.y;
        }

        public void SetAspectRatio(float aspectRatio)
        {
            _aspectRatio.Value = aspectRatio;
        }

        public void SetDisplayMode(DisplayMode displayMode)
        {
            _displayMode.Value = displayMode;
        }

        public void SetAvailableResolutions(List<Vector2Int> resolutions)
        {
            _availableResolutions.Value = resolutions;
        }

        public void SetAvailableDisplayModes(List<DisplayMode> displayModes)
        {
            _availableDisplayModes.Value = displayModes;
        }
    }
}
