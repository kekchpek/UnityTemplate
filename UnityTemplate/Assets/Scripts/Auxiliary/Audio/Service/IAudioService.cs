using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace AudioSystem.Service
{
    public interface IAudioService
    {
        UniTask Initialize();
        void PlaySFX(string id, bool isLoop = false);
        void SetSFXVolume(float volume);
        void SetMusic(string id, bool loop = true);
        void StopMusic();
        void PauseNonMusic();
        void UnpauseNonMusic();
        void PlayDialogue(string id);
        void StopDialogue();
        UniTask PlaySFXAsync(IEnumerable<string> sequenceNames);
        float GetClipLength(string id);

        /// <summary>
        /// RMS loudness (0–1) of the current dialogue clip near the current playhead time.
        /// </summary>
        float GetDialogueClipLoudnessAtPlayhead();
    }
}