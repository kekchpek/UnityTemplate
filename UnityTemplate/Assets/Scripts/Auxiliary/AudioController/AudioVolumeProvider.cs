using UnityEngine;

namespace kekchpek.Auxiliary.AudioSystem
{
    public static class AudioVolumeProvider
    {
        private static float _effectsVolume = 1f;
        public static float EffectsVolume => _effectsVolume;

        public static void SetEffectsVolume(float volume)
        {
            _effectsVolume = Mathf.Clamp01(volume);
        }
    }
}
