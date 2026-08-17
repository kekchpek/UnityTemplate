using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AudioSystem.Container
{
    [CreateAssetMenu(fileName = "AudioClipsContainer", menuName = "Audio/Clips Container")]
    public class AudioClipsContainer : ScriptableObject, IAudioClipsContainer
    {
        [Serializable]
        private class ClipElement
        {
            [SerializeField]
            private string id;

            [SerializeField]
            private AudioClip clip;

            [SerializeField]
            private List<AudioClip> clips;

            public string Id => id;
            public AudioClip Clip => clip;
            public List<AudioClip> Clips => clips;
        }

        [SerializeField]
        private ClipElement[] content;

        public AudioClip GetSingle(string id)
        {
            var element = FindClipElement(id);
            if (element == null)
            {
                Debug.LogWarning($"Clip with id {id} not found.");
                return null;
            }

            if (element.Clip != null)
            {
                return element.Clip;
            }

            if (element.Clips != null && element.Clips.Count > 0)
            {
                return element.Clips[Random.Range(0, element.Clips.Count)];
            }

            Debug.LogWarning($"Clip with id {id} has no clip configured.");
            return null;
        }

        public AudioClip GetRandom(string id)
        {
            var element = FindClipElement(id);
            if (element == null)
            {
                Debug.LogWarning($"Clip with id {id} not found.");
                return null;
            }

            if (element.Clips != null && element.Clips.Count > 0)
            {
                return element.Clips[Random.Range(0, element.Clips.Count)];
            }

            if (element.Clip != null)
            {
                return element.Clip;
            }

            Debug.LogWarning($"Clip with id {id} has no clip configured.");
            return null;
        }

        private ClipElement FindClipElement(string id)
        {
            foreach (var clipElement in content)
            {
                if (clipElement.Id.Equals(id))
                {
                    return clipElement;
                }
            }

            return null;
        }
    }
}