using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AuxiliaryComponents
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Image))]
    public class ImagePpuScaler : UIBehaviour
    {

        private enum ScaleAxis
        {
            // ReSharper disable once UnusedMember.Local
            X,
            Y
        }

        [SerializeField] private float _scaleFactor = 1f;

        [SerializeField] private ScaleAxis _scaleAxis = ScaleAxis.Y;

        [SerializeField]
        private Image _image;

        protected override void Awake()
        {
            UpdatePpu();
        }

        private void Update()
        {
            UpdatePpu();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            UpdatePpu();
        }

        protected override void OnTransformParentChanged()
        {
            UpdatePpu();
        }

        private void OnTransformChildrenChanged()
        {
            UpdatePpu();
        }

        private void UpdatePpu()
        {
            if (!_image)
                _image = GetComponent<Image>();
            if (!_image.sprite)
                return;
            var border = _image.sprite.border;
            var maxBorder = Mathf.Max(
                Mathf.Max(border.x, border.y), 
                Mathf.Max(border.z, border.w));
            float imgSize;
            if (_scaleAxis == ScaleAxis.Y)
            {
                imgSize = _image.rectTransform.rect.size.y;
            }
            else
            {
                imgSize = _image.rectTransform.rect.size.x;
            }

            _image.pixelsPerUnitMultiplier = maxBorder / (imgSize / 2f) * _scaleFactor;
        }
    }
}