using UnityEngine;

namespace AuxiliaryComponents
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class RelativeMover : MonoBehaviour
    {
        
        [SerializeField] [Range(0, 1f)] private float _relativePositionX;
        [SerializeField] [Range(0, 1f)] private float _relativePositionY;
#if UNITY_EDITOR
        [SerializeField] private bool _executeInEditor;
#endif

        private void Update()
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying && !_executeInEditor)
                return;
            #endif
            
            var cachedTrans = transform;
            cachedTrans.localPosition = new Vector3(
                Screen.width * _relativePositionX,
                Screen.height * _relativePositionY,
                cachedTrans.localPosition.z)
                - 0.5f * new Vector3(Screen.width, Screen.height, 0f);
        }
    }
}