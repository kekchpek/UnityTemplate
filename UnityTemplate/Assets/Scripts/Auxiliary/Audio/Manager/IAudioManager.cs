using AsyncReactAwait.Bindable;
using UnityEngine;

namespace AudioSystem.Manager
{
    public interface IAudioManager
    {
        IMutable<float> MasterVolume { get; }
        IMutable<float> MusicVolume { get; }
        IMutable<float> SfxVolume { get; }
        IMutable<float> EffectVolume { get; }
        IMutable<float> DialogueVolume { get; }

        void PlaySfx(AudioClip audioClip, bool loop = false);
        void StopLoopingSfx(AudioClip audioClip);
        void SetMusic(AudioClip audioClip, bool loop = false);
        void SetFxVolume(float volume);
        void PauseMusic();
        void StartMusic();
        void PauseNonMusic();
        void UnpauseNonMusic();
        void PlayDialogue(AudioClip audioClip);
        void StopDialogue();
        float GetClipLength(AudioClip clip);

        /// <summary>
        /// RMS level (0–1) of the dialogue AudioSource output.
        /// Returns 0 if nothing is playing.
        /// </summary>
        float GetDialogueClipLoudnessAtPlayhead();
    }
}