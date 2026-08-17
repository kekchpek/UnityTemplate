using AudioSystem.Manager;
using AudioSystem.Container;
using BasicSettings;
using AssetsSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System;
using Diagnostics.Time;

namespace AudioSystem.Service
{
    public class AudioService : IAudioService
    {
        private readonly IAssetsModel _assetsModel;
        private readonly IAudioManager _audioManager;
        private readonly ISettingsModel _settingsModel;
        private IAudioClipsContainer _clipsContainer;

        public AudioService(
            IAssetsModel assetsModel, 
            IAudioManager audioManager, 
            ISettingsModel settingsModel)
        {
            _assetsModel = assetsModel;
            _audioManager = audioManager;
            _settingsModel = settingsModel;
        }

        public async UniTask Initialize()
        {
            using var timeDebug = TimeDebug.StartMeasure("AudioService.Initialize");
            _clipsContainer = await _assetsModel.LoadAsset<AudioClipsContainer>("Audio/AudioClipsContainer");
            BindToSettingsService();
        }

        public void PlaySFX(string id, bool isLoop = false)
        {
            var clip = _clipsContainer.GetSingle(id);
            if (clip != null)
            {
                _audioManager.PlaySfx(clip, isLoop);
            }
            else 
            {
                Debug.LogError($"AudioService.PlaySFX: Clip '{id}' not found in container");
            }
        }

        public UniTask PlaySFXAsync(IEnumerable<string> sequenceNames)
        {
            foreach (var sequenceName in sequenceNames)
            {
                var clip = _clipsContainer.GetSingle("Combo"+sequenceName);
                
                if (clip != null)
                {
                    _audioManager.PlaySfx(clip);
                }
            }
            
            return UniTask.CompletedTask;
        }

        public void SetSFXVolume(float volume)
        {
            _audioManager.SetFxVolume(volume);
        }

        public void SetMusic(string id, bool loop = true)
        {
            if (_clipsContainer == null)
            {
                Debug.LogWarning($"AudioService.SetMusic: ClipsContainer is null, cannot play {id}");
                return;
            }

            var clip = _clipsContainer.GetSingle(id);
            if (clip != null)
            {
                _audioManager.SetMusic(clip, loop);
            }
            else
            {
                Debug.LogWarning($"AudioService.SetMusic: Clip '{id}' not found in container");
            }
        }

        public void StopMusic()
        {
            _audioManager.SetMusic(null, false);
        }

        public void PauseNonMusic()
        {
            _audioManager.PauseNonMusic();
        }

        public void UnpauseNonMusic()
        {
            _audioManager.UnpauseNonMusic();
        }

        public float GetClipLength(string id)
        {
            var clip = _clipsContainer.GetSingle(id);
            if (clip == null) return 0f;
            return _audioManager.GetClipLength(clip);
        }

        public float GetDialogueClipLoudnessAtPlayhead()
        {
            return _audioManager.GetDialogueClipLoudnessAtPlayhead();
        }

        public void PlayDialogue(string id)
        {
            var clip = _clipsContainer.GetSingle(id);
            if (clip != null)
            {
                _audioManager.PlayDialogue(clip);
            }
            else
            {
                Debug.LogError($"AudioService.PlayDialogue: Clip '{id}' not found in container");
            }
        }

        public void StopDialogue()
        {
            _audioManager.StopDialogue();
        }

        private void BindToSettingsService()
        {
            _settingsModel.MasterVolume.Bind(volume => UpdateMasterVolume(), false);
            _settingsModel.IsAudioMasterEnabled.Bind(enabled => UpdateMasterVolume(), false);
            _settingsModel.MusicVolume.Bind(volume => UpdateMusicVolume(), false);
            _settingsModel.IsAudioMusicEnabled.Bind(enabled => UpdateMusicVolume(), false);
            _settingsModel.EffectsVolume.Bind(volume => UpdateEffectsVolume(), false);
            _settingsModel.IsAudioFxEnabled.Bind(enabled => UpdateEffectsVolume(), false);
            _settingsModel.DialogueVolume.Bind(volume => UpdateDialogueVolume(), false);
            _settingsModel.IsAudioDialogueEnabled.Bind(enabled => UpdateDialogueVolume(), false);
            
            UpdateMasterVolume();
            UpdateMusicVolume();
            UpdateEffectsVolume();
            UpdateDialogueVolume();
        }

        private void UpdateMasterVolume()
        {
            var effectiveVolume = _settingsModel.IsAudioMasterEnabled.Value ? _settingsModel.MasterVolume.Value / 100f : 0f;
            _audioManager.MasterVolume.Value = effectiveVolume;
        }

        private void UpdateMusicVolume()
        {
            var effectiveVolume = _settingsModel.IsAudioMusicEnabled.Value ? _settingsModel.MusicVolume.Value / 100f : 0f;
            _audioManager.MusicVolume.Value = effectiveVolume;
        }

        private void UpdateEffectsVolume()
        {
            var effectiveVolume = _settingsModel.IsAudioFxEnabled.Value ? _settingsModel.EffectsVolume.Value / 100f : 0f;
            _audioManager.SfxVolume.Value = effectiveVolume;
        }

        private void UpdateDialogueVolume()
        {
            var effectiveVolume = _settingsModel.IsAudioDialogueEnabled.Value ? _settingsModel.DialogueVolume.Value / 100f : 0f;
            _audioManager.DialogueVolume.Value = effectiveVolume;
        }

    }
}