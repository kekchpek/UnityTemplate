using UnityEngine;

namespace AudioSystem.Container
{
    public interface IAudioClipsContainer
    {
        AudioClip GetSingle(string id);
        AudioClip GetRandom(string id);
    }
}