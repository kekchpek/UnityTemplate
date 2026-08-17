using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace kekchpek.AuxiliaryComponents.SizeFitters
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class TextSizeFitter : MonoBehaviour
    {
        public enum FitMode
        {
            Unconstrained,
            MinSize,
            PreferredSize,
        }

        [SerializeField]
        private TMP_Text _explicitText;

        [SerializeField]
        private FitMode _horizontalFit = FitMode.Unconstrained;

        [SerializeField]
        private FitMode _verticalFit = FitMode.Unconstrained;

        [SerializeField]
        private float _maxWidth = -1f;

        private TMP_Text _text;
        private RectTransform _rectTransform;
        private bool _refreshing;

        private void Awake()
        {
            Cache();
        }

        private void OnEnable()
        {
            Cache();
            if (_text != null)
                _text.RegisterDirtyLayoutCallback(OnTextLayoutDirty);
            RefreshSize();
        }

        private void OnDisable()
        {
            if (_text != null)
                _text.UnregisterDirtyLayoutCallback(OnTextLayoutDirty);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Cache();
            if (!Application.isPlaying && isActiveAndEnabled)
                RefreshSize();
        }
#endif

        private void OnRectTransformDimensionsChange()
        {
            RefreshSize();
        }

        private void Cache()
        {
            if (_explicitText) {
                _text = _explicitText;
            } else {
                _text = GetComponent<TMP_Text>();
            }
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnTextLayoutDirty()
        {
            RefreshSize();
        }

        private void RefreshSize()
        {
            if (!isActiveAndEnabled || _text == null || _rectTransform == null)
                return;
            if (_horizontalFit == FitMode.Unconstrained && _verticalFit == FitMode.Unconstrained && _maxWidth < 0f)
                return;
            if (_refreshing)
                return;

            _refreshing = true;

            _text.ForceMeshUpdate(true);
            var r = _rectTransform.rect;
            float w = r.width;
            float h = r.height;

            if (_horizontalFit != FitMode.Unconstrained)
                w = ResolveAxis(_horizontalFit, true);

            w = ApplyMaxWidth(w);

            if (!Mathf.Approximately(w, r.width))
            {
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                _text.ForceMeshUpdate(true);
            }

            if (_verticalFit != FitMode.Unconstrained)
                h = ResolveAxis(_verticalFit, false);

            if (float.IsNaN(w) || float.IsNaN(h))
            {
                var fallback = GetPreferredValuesFallback();
                if (float.IsNaN(w))
                    w = ApplyMaxWidth(fallback.x);
                if (float.IsNaN(h))
                    h = fallback.y;
            }

            r = _rectTransform.rect;
            if (!Mathf.Approximately(r.width, w) || !Mathf.Approximately(r.height, h))
            {
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }

            _refreshing = false;
        }

        private float ResolveAxis(FitMode fit, bool horizontal)
        {
            float value = horizontal
                ? (fit == FitMode.MinSize
                    ? LayoutUtility.GetMinWidth(_rectTransform)
                    : LayoutUtility.GetPreferredWidth(_rectTransform))
                : (fit == FitMode.MinSize
                    ? LayoutUtility.GetMinHeight(_rectTransform)
                    : LayoutUtility.GetPreferredHeight(_rectTransform));

            if (value > 0f || fit == FitMode.MinSize)
                return value;

            var p = GetPreferredValuesFallback();
            return horizontal ? p.x : p.y;
        }

        private float ApplyMaxWidth(float width)
        {
            if (_maxWidth < 0f)
                return width;
            return Mathf.Min(width, _maxWidth);
        }

        private Vector2 GetPreferredValuesFallback()
        {
            if (_maxWidth < 0f)
                return _text.GetPreferredValues();
            return _text.GetPreferredValues(_maxWidth, 0f);
        }
    }
}
