using System;
using System.Collections.Generic;
using System.Linq;
using AsyncReactAwait.Bindable;
using UnityEngine;

namespace AudioSystem.Manager
{
    public class AudioManager : MonoBehaviour, IAudioManager
    {
        [SerializeField] private AudioSource _sfxAudioSource;
        [SerializeField] private AudioSource _musicAudioSource;
        [SerializeField] private AudioSource _dialogueAudioSource;

        public IMutable<float> MasterVolume { get; } = new Mutable<float>(1f);
        public IMutable<float> MusicVolume { get; } = new Mutable<float>(1f);
        public IMutable<float> SfxVolume { get; } = new Mutable<float>(1f);
        public IMutable<float> EffectVolume => SfxVolume;
        public IMutable<float> DialogueVolume { get; } = new Mutable<float>(1f);
        
        private readonly Dictionary<string, AudioSource> _loopingSfxAudioSources = new();
        private readonly Dictionary<string, float> _targetVolumes = new();

        private const int DialogueLoudnessWindowFrames = 512;
        private float[] _dialogueClipLoudnessSampleBuffer;

        private void Awake()
        {
            transform.SetParent(null);
            
            MasterVolume.Bind(OnMasterVolumeChanged);
            SfxVolume.Bind(OnSfxVolumeChanged);
            MusicVolume.Bind(OnMusicVolumeChanged);
            DialogueVolume.Bind(OnDialogueVolumeChanged);
        }

        public void PlaySfx(AudioClip audioClip, bool loop = false)
        {
            if (audioClip == null) return;

            if (loop)
            {
                if (_loopingSfxAudioSources.TryGetValue(audioClip.name, out var loopingSfxAudioSource))
                {
                    loopingSfxAudioSource.gameObject.SetActive(true);
                    loopingSfxAudioSource.Play();
                }
                else
                {
                    var newLoopingSource = Instantiate(_sfxAudioSource.gameObject, transform)
                        .GetComponent<AudioSource>();
                    newLoopingSource.clip = audioClip;
                    newLoopingSource.loop = true;
                    newLoopingSource.name = audioClip.name;
                    newLoopingSource.volume = SfxVolume.Value;
                    newLoopingSource.Play();
                    _loopingSfxAudioSources[audioClip.name] = newLoopingSource;
                }
            }
            else
            {
                _sfxAudioSource.PlayOneShot(audioClip);
            }
        }

        public void StopLoopingSfx(AudioClip audioClip)
        {
            if (audioClip != null && _loopingSfxAudioSources.TryGetValue(audioClip.name, out var loopingSfxAudioSource))
            {
                loopingSfxAudioSource.gameObject.SetActive(false);
                loopingSfxAudioSource.Stop();
            }
        }

        public void SetMusic(AudioClip audioClip, bool loop = false)
        {
            if (_musicAudioSource != null)
            {
            _musicAudioSource.clip = audioClip;
            _musicAudioSource.loop = loop;
            _musicAudioSource.time = 0f;
                _musicAudioSource.Play();
            }
        }

        public void SetFxVolume(float volume)
        {
            SfxVolume.Value = Mathf.Clamp(volume, 0f, 1f);
        }

        public void PauseMusic()
        {
            _musicAudioSource?.Pause();
        }

        public void StartMusic()
        {
            _musicAudioSource?.Play();
        }

        public void PauseNonMusic()
        {
            if (_sfxAudioSource)
                _sfxAudioSource.Pause();
            foreach (var loopingSource in _loopingSfxAudioSources.Values)
            {
                if (loopingSource)
                {
                    loopingSource.Pause();
                }
            }
            if (_dialogueAudioSource)
                _dialogueAudioSource.Pause();
        }

        public void UnpauseNonMusic()
        {
            if (_sfxAudioSource)
                _sfxAudioSource.UnPause();
            foreach (var loopingSource in _loopingSfxAudioSources.Values)
            {
                if (loopingSource)
                {
                    loopingSource.UnPause();
                }
            }
            if (_dialogueAudioSource)
                _dialogueAudioSource.UnPause();
        }

        public float GetClipLength(AudioClip clip)
        {
            if (clip == null) return 0f;
            return clip.length;
        }

        public float GetDialogueClipLoudnessAtPlayhead()
        {
            if (_dialogueAudioSource == null || !_dialogueAudioSource.isPlaying)
                return 0f;

            if (_dialogueClipLoudnessSampleBuffer == null || _dialogueClipLoudnessSampleBuffer.Length < DialogueLoudnessWindowFrames)
                _dialogueClipLoudnessSampleBuffer = new float[DialogueLoudnessWindowFrames];

            _dialogueAudioSource.GetOutputData(_dialogueClipLoudnessSampleBuffer, 0);

            double sumSquares = 0d;
            for (int i = 0; i < DialogueLoudnessWindowFrames; i++)
            {
                float s = _dialogueClipLoudnessSampleBuffer[i];
                sumSquares += s * (double)s;
            }

            return (float)Math.Sqrt(sumSquares / DialogueLoudnessWindowFrames);
        }

        public void PlayDialogue(AudioClip audioClip)
        {
            if (audioClip == null || _dialogueAudioSource == null)
                return;

            _dialogueAudioSource.Stop();
            _dialogueAudioSource.clip = audioClip;
            _dialogueAudioSource.loop = false;
            _dialogueAudioSource.time = 0f;
            _dialogueAudioSource.Play();
        }

        public void StopDialogue()
        {
            if (_dialogueAudioSource != null)
            {
                _dialogueAudioSource.Stop();
            }
        }

        private void OnMasterVolumeChanged(float masterVolume)
        {
            OnSfxVolumeChanged(SfxVolume.Value);
            OnMusicVolumeChanged(MusicVolume.Value);
            OnDialogueVolumeChanged(DialogueVolume.Value);
        }

        private void OnSfxVolumeChanged(float volume)
        {
            var finalVolume = Mathf.Clamp(volume * MasterVolume.Value, 0f, 1f);
            
            if (_sfxAudioSource != null)
            {
                _sfxAudioSource.volume = finalVolume;
            }

            foreach (var loopingSource in _loopingSfxAudioSources.Values)
            {
                if (loopingSource != null)
                {
                    loopingSource.volume = finalVolume;
                }
            }
        }

        private void OnMusicVolumeChanged(float volume)
        {
            var finalVolume = Mathf.Clamp(volume * MasterVolume.Value, 0f, 1f);
            
            if (_musicAudioSource != null)
            {
                _musicAudioSource.volume = finalVolume;
            }
        }

        private void OnDialogueVolumeChanged(float volume)
        {
            var finalVolume = Mathf.Clamp(volume * MasterVolume.Value, 0f, 1f);
            
            if (_dialogueAudioSource != null)
            {
                _dialogueAudioSource.volume = finalVolume;
            }
        }
    }
}