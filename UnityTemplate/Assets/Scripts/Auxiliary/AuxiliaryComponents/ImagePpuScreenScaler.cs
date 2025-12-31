using UnityEngine;
using UnityEngine.UI;

namespace kekchpek.Auxiliary.AuxiliaryComponents
{
    [RequireComponent(typeof(Image))]
    public class ImagePpuScreenScaler : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField] 
        private Image _image;

        [SerializeField] private float _targetRatio;
        [SerializeField] private float _targetPpu;
        [SerializeField] private bool _widthOnHeight;
        [SerializeField] private bool _inverted;
        [SerializeField] private float _minScale;
        [SerializeField] private float _maxScale;

        private void Awake() {
            Update();
        }

        private void Init() {
            _image = GetComponent<Image>();
        }
        
        private void Reset() {
            Init();
        }

        private void OnValidate() {
            Init();
        }

        private void Update() {
            float ratio;
            if (_widthOnHeight) 
            {
                ratio = (float)Screen.width / Screen.height;
            }
            else {
                ratio = (float)Screen.height / Screen.width;
            }
            var scale = ratio / _targetRatio;
            if (_inverted) {    
                scale = 1f / scale;
            }
            scale = Mathf.Clamp(scale, _minScale, _maxScale);
            _image.pixelsPerUnitMultiplier = _targetPpu * scale;
        }
    }
}