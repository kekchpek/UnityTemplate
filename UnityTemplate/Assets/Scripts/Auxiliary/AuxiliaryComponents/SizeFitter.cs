using UnityEngine;

namespace AuxiliaryComponents
{
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class SizeFitter : MonoBehaviour
    {

        private enum FitMode
        {
            VerticalContent,
            HorizontalContent
        }
        
        private DrivenRectTransformTracker _driver = new();

        [SerializeField] private FitMode _fitMode;
        [SerializeField] private float _fitScale = 1f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            Fit();
        }
        
        private void Update()
        {
            Fit();
        }

        private void Fit()
        {
            _driver.Clear();
            var child = (RectTransform)transform.GetChild(0);
            switch (_fitMode)
            {
                case FitMode.VerticalContent:
                {
                    _driver.Add(this, _rectTransform, DrivenTransformProperties.SizeDeltaY);
                    _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, child.rect.height * _fitScale);
                    break;
                }
                case FitMode.HorizontalContent:
                {
                    _driver.Add(this, _rectTransform, DrivenTransformProperties.SizeDeltaX);
                    _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, child.rect.width * _fitScale);
                    break;
                }
            }
        }
    }
}