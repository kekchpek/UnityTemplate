using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.AudioSystem
{
    [Serializable]
    public class AudioSequence
    {
        public string SequenceName;
        public List<AudioData> AudioClips = new();
        public bool Randomize = false;
        
        private int _currentIndex = 0;
        
        public AudioData GetNextAudioData()
        {
            if (AudioClips == null || AudioClips.Count == 0)
                return null;
                
            AudioData audioData;
            
            if (Randomize)
            {
                audioData = AudioClips[UnityEngine.Random.Range(0, AudioClips.Count)];
            }
            else
            {
                audioData = AudioClips[_currentIndex];
                _currentIndex = (_currentIndex + 1) % AudioClips.Count;
            }
            
            return audioData;
        }
    }
}
