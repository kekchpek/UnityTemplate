using UnityEngine;

namespace AuxiliaryComponents
{

    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class ScreenScaler : MonoBehaviour
    {

        public enum Mode 
        {
            Fit,
            FitAndFillWidth,
            FitAndFillHeight,
            FitFillBoth,
        }

        [SerializeField] private RectTransform _target;
        [SerializeField] private Mode _mode;
        [SerializeField] private Vector2 _targetSize;
        
        private void Awake()
        {
            _target = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_mode == Mode.Fit)
            {
                Fit();
            }
            else if (_mode == Mode.FitAndFillWidth)
            {
                FitAndFillWidth();
            }
            else if (_mode == Mode.FitAndFillHeight)
            {
                FitAndFillHeight();
            }
            else if (_mode == Mode.FitFillBoth)
            {
                StretchFromTarget();
            }
        }

        private void Fit()
        {
            _target.anchoredPosition = Vector2.zero;
            var size = _target.sizeDelta;
            var scale = Vector3.one * Mathf.Min(Screen.width / size.x, Screen.height / size.y);
            _target.localScale = scale;
        }

        private void FitAndFillWidth()
        {
            _target.anchoredPosition = Vector2.zero;
            var size = _targetSize;
            var scaleFactor = Screen.height / size.y;
            var scale = Vector3.one * scaleFactor;
            _target.localScale = scale;
            _target.sizeDelta = new Vector2(Screen.width / scaleFactor, size.y);
        }

        private void FitAndFillHeight()
        {
            _target.anchoredPosition = Vector2.zero;
            var size = _targetSize;
            var scaleFactor = Screen.width / size.x;
            var scale = Vector3.one * scaleFactor;
            _target.localScale = scale;
            _target.sizeDelta = new Vector2(size.x, Screen.height / scaleFactor);
        }

        private void StretchFromTarget()
        {
            var screenRatio = (float)Screen.width / Screen.height;
            var targetRatio = _targetSize.x / _targetSize.y;
            if (screenRatio > targetRatio)
            {
                FitAndFillWidth();
            }
            else
            {
                FitAndFillHeight();
            }
        }
    }
}