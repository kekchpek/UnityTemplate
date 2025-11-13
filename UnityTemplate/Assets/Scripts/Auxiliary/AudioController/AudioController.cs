using System.Collections.Generic;
using System.Linq;
using kekchpek.Auxiliary.AudioSystem;
using UnityEngine;

namespace kekchpek.Auxiliary
{
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private List<AudioSequence> _sequences = new();
        [SerializeField] private AudioSource _oneShotAudioSource;
        [SerializeField] private AudioSource _loopingAudioSource;
        
        private string _activeSequentialSequence = null;
        private AudioSequence _currentSequentialSequence = null;

        private void Start()
        {
            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (_oneShotAudioSource == null)
            {
                _oneShotAudioSource = gameObject.AddComponent<AudioSource>();
                _oneShotAudioSource.playOnAwake = false;
                _oneShotAudioSource.loop = false;
            }
            
            if (_loopingAudioSource == null)
            {
                _loopingAudioSource = gameObject.AddComponent<AudioSource>();
                _loopingAudioSource.playOnAwake = false;
                _loopingAudioSource.loop = true;
            }
        }

        public bool HasSequence(string sequenceName)
        {
            return _sequences.Any(s => s.SequenceName == sequenceName);
        }

        public void PlaySequence(string sequenceName)
        {
            InitializeAudioSources();
            
            var sequence = _sequences.FirstOrDefault(s => s.SequenceName == sequenceName);
            if (sequence == null)
            {
                return;
            }

            var audioData = sequence.GetNextAudioData();
            if (audioData?.AudioClip == null)
            {
                return;
            }

            PlayAudioData(audioData);
        }

        public void StopLoopingSequence(string sequenceName)
        {
            InitializeAudioSources();
            
            var sequence = _sequences.FirstOrDefault(s => s.SequenceName == sequenceName);
            if (sequence == null) return;

            var hasLoopingAudio = sequence.AudioClips?.Any(audioData => audioData.IsLooping) == true;
            if (hasLoopingAudio && _loopingAudioSource != null)
            {
                _loopingAudioSource.Stop();
            }
        }

        public void StopAllLooping()
        {
            InitializeAudioSources();
            
            if (_loopingAudioSource != null)
            {
                _loopingAudioSource.Stop();
            }
        }

        public void StartContinuousSequence(string sequenceName)
        {
            var sequence = _sequences.FirstOrDefault(s => s.SequenceName == sequenceName);
            if (sequence == null || sequence.AudioClips == null || sequence.AudioClips.Count == 0)
                return;

            _activeSequentialSequence = sequenceName;
            _currentSequentialSequence = sequence;
            
            PlayNextSequentialClip();
        }

        public void StopContinuousSequence(string sequenceName)
        {
            if (_activeSequentialSequence == sequenceName)
            {
                _activeSequentialSequence = null;
                _currentSequentialSequence = null;
            }
        }

        private void PlayNextSequentialClip()
        {
            if (_currentSequentialSequence == null || string.IsNullOrEmpty(_activeSequentialSequence))
                return;

            var audioData = _currentSequentialSequence.GetNextAudioData();
            if (audioData?.AudioClip == null)
                return;

            PlayAudioData(audioData);
            
            StartCoroutine(WaitAndPlayNext(audioData.AudioClip.length));
        }

        private System.Collections.IEnumerator WaitAndPlayNext(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!string.IsNullOrEmpty(_activeSequentialSequence))
            {
                PlayNextSequentialClip();
            }
        }

        private void PlayAudioData(AudioData audioData)
        {
            if (audioData.AudioClip == null)
                return;

            float effectsVolume = AudioVolumeProvider.EffectsVolume;
            float finalVolume = audioData.Volume * effectsVolume;
            
            if (audioData.IsLooping)
            {
                if (_loopingAudioSource == null) return;
                
                _loopingAudioSource.clip = audioData.AudioClip;
                _loopingAudioSource.volume = finalVolume;
                _loopingAudioSource.pitch = 1f;
                _loopingAudioSource.Play();
            }
            else
            {
                if (_oneShotAudioSource == null) return;
                
                _oneShotAudioSource.volume = finalVolume;
                _oneShotAudioSource.pitch = 1f;
                _oneShotAudioSource.PlayOneShot(audioData.AudioClip);
            }
        }

        public AudioSequence GetSequence(string sequenceName)
        {
            return _sequences.FirstOrDefault(s => s.SequenceName == sequenceName);
        }

        public List<string> GetSequenceNames()
        {
            return _sequences.Select(s => s.SequenceName).ToList();
        }
    }
}
