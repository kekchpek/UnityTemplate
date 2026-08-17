using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AudioSystem.Service;

namespace AudioSystem.NoAudio
{
    public class NoAudioService : IAudioService
    {
        public float GetClipLength(string id)
        {
            return 0f;
        }

        public float GetDialogueClipLoudnessAtPlayhead()
        {
            return 0f;
        }

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void PauseNonMusic()
        {
        }

        public void PlayDialogue(string id)
        {
        }

        public void PlaySFX(string id)
        {
        }

        public void PlaySFX(string id, bool isLoop = false)
        {
        }

        public UniTask PlaySFXAsync(IEnumerable<string> sequenceNames)
        {
            return UniTask.CompletedTask;
        }

        public void SetMusic(string id, bool loop = true)
        {
        }

        public void SetSFXVolume(float volume)
        {
        }

        public void StopDialogue()
        {
        }

        public void StopMusic()
        {
        }

        public void UnpauseNonMusic()
        {
        }
    }
}