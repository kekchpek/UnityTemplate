using System.Collections.Generic;
using System.Linq;
using System;
using BasicSettings.Data;
using Cysharp.Threading.Tasks;
using kekchpek.Auxiliary.Time;
using UnityEngine;

namespace BasicSettings
{
    public class SettingsService : ISettingsService, IDisposable
    {
        private readonly ISettingsModel _model;
        private readonly ITimeManager _timeManager;
        private bool _isInitialized = false;
        
        public SettingsService(
            ISettingsModel model,
            ITimeManager timeManager
            )
        {
            _model = model;
            _timeManager = timeManager;
        }

        public async UniTask Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _model.SetAvailableResolutions(BuildAvailableResolutions());
            _model.SetAvailableDisplayModes(BuildAvailableDisplayModes());
            ApplyDisplayFromModel();
            _isInitialized = true;
            _timeManager.CurrentTimestampUtc.Bind(OnTick);
            await UniTask.CompletedTask;
        }

        private void OnTick()
        {
            var currentResolution = new Vector2Int(Screen.width, Screen.height);
            if (_model.Resolution.Value != currentResolution)
            {
                _model.SetResolution(currentResolution);
            }
            ApplyDisplayMode(_model.DisplayMode.Value);
        }

        private static List<Vector2Int> BuildAvailableResolutions()
        {
            var seen = new HashSet<(int width, int height)>();
            var result = new List<Vector2Int>();
            foreach (var resolution in Screen.resolutions)
            {
                var key = (resolution.width, resolution.height);
                if (!seen.Add(key))
                    continue;
                // We decided, that extremely small resolutions are not needed
                if (resolution.width < 800)
                    continue;
                result.Add(new Vector2Int(resolution.width, resolution.height));
            }

            result.Sort((a, b) => -(b.x * b.y).CompareTo(a.x * a.y));
            return result;
        }

        private static List<DisplayMode> BuildAvailableDisplayModes()
        {
            return new List<DisplayMode>
            {
                DisplayMode.Fullscreen,
                DisplayMode.Borderless,
                DisplayMode.Windowed
            };
        }

        private void ApplyDisplayFromModel()
        {
            if (_model.DisplayMode != null)
                ApplyDisplayMode(_model.DisplayMode.Value);
            if (_model.Resolution != null)
                ApplyResolution(_model.Resolution.Value);
        }

        public void SetEnableAudioMaster(bool enable)
        {
            _model.SetAudioMasterEnabled(enable);
        }
        public void SetEnableAudioMusic(bool enable)
        {
            _model.SetAudioMusicEnabled(enable);
        }
        public void SetEnableAudioFX(bool enable)
        {
            _model.SetAudioFxEnabled(enable);
        }

        public void SetMasterVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp(volume, 0f, 100f);
            _model.SetMasterVolume(clampedVolume);
        }

        public void SetMusicVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp(volume, 0f, 100f);
            _model.SetMusicVolume(clampedVolume);
        }

        public void SetEffectsVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp(volume, 0f, 100f);
            _model.SetEffectsVolume(clampedVolume);
        }

        public void SetEnableAudioDialogue(bool enable)
        {
            _model.SetAudioDialogueEnabled(enable);
        }

        public void SetDialogueVolume(float volume)
        {
            var clampedVolume = Mathf.Clamp(volume, 0f, 100f);
            _model.SetDialogueVolume(clampedVolume);
        }

        public void SetResolution(Vector2Int resolution)
        {
            _model.SetResolution(resolution);
            ApplyResolution(resolution);
        }

        public void SetAspectRatio(float aspectRatio)
        {
            _model.SetAspectRatio(aspectRatio);
        }

        public void SetDisplayMode(DisplayMode displayMode)
        {
            _model.SetDisplayMode(displayMode);
            ApplyDisplayMode(displayMode);
        }

        private void ApplyDisplayMode(DisplayMode displayMode)
        {
            switch (displayMode)
            {
                case DisplayMode.Fullscreen:
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    Screen.fullScreen = true;
                    break;
                case DisplayMode.Borderless:
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    Screen.fullScreen = true;
                    break;
                case DisplayMode.Windowed:
                    Screen.fullScreen = false;
                    break;
            }
        }

        private void ApplyResolution(Vector2Int resolution)
        {
            var matchingResolution = Screen.resolutions
                .Where(r => r.width == resolution.x && r.height == resolution.y)
                .OrderByDescending(r => r.refreshRateRatio.value)
                .FirstOrDefault();

            if (matchingResolution.width > 0)
            {
                Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode, matchingResolution.refreshRateRatio);
            }
            else
            {
                Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);
            }
        }

        public void Dispose()
        {
            _timeManager.CurrentTimestampUtc.Unbind(OnTick);
        }
    }
}