using System;
using UnityEngine;

namespace kekchpek.Auxiliary.AudioSystem
{
    [Serializable]
    public class AudioData
    {
        [SerializeField] private AudioClip _audioClip;
        [SerializeField] private float _volume = 1f;
        [SerializeField] private bool _isLooping = false;

        public AudioClip AudioClip => _audioClip;
        public float Volume => _volume;
        public bool IsLooping => _isLooping;
    }
}
