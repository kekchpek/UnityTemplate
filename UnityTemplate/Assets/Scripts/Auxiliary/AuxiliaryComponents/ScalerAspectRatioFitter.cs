using UnityEngine;

namespace AuxiliaryComponents
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class ScalerAspectRatioFitter : MonoBehaviour
    {
        public enum Mode
        {
            Fit,
        }

        [SerializeField]
        private RectTransform _target;

        [SerializeField]
        private Mode _mode;

        private DrivenRectTransformTracker _driver = new();

        private void Awake()
        {
            if (_target == null)
                _target = GetComponent<RectTransform>();
        }

        private void OnDisable()
        {
            _driver.Clear();
        }

        private void Update()
        {
            if (_mode == Mode.Fit)
                Fit();
        }

        private void Fit()
        {
            var parent = _target.parent as RectTransform;
            if (parent == null)
            {
                _driver.Clear();
                return;
            }

            var parentRect = parent.rect;
            var targetRect = _target.rect;
            if (targetRect.width <= 0f || targetRect.height <= 0f)
            {
                _driver.Clear();
                return;
            }

            _driver.Clear();
            _driver.Add(this, _target, DrivenTransformProperties.Scale);

            float scaleX = parentRect.width / targetRect.width;
            float scaleY = parentRect.height / targetRect.height;
            float scale = Mathf.Min(scaleX, scaleY);
            _target.localScale = Vector3.one * scale;
        }
    }
}
