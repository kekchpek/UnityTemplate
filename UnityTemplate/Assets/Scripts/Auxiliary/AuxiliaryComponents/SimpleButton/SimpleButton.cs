using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace kekchpek.AuxiliaryComponents.SimpleButton
{
    [RequireComponent(typeof(Image))]
    public class SimpleButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Graphic _targetGraphic;

        [SerializeField]
        private bool _interactable = true;

        [SerializeField]
        private Button.ButtonClickedEvent _onClick = new Button.ButtonClickedEvent();

        [SerializeField]
        private bool _useColorTransition = true;

        [SerializeField]
        private SimpleButtonColorBlendMode _colorBlendMode = SimpleButtonColorBlendMode.Override;

        [SerializeField]
        private Color _baseColor = Color.white;

        [Header("State colors")]
        [SerializeField]
        private Color _normalColor = Color.white;

        [SerializeField]
        private Color _normalDisabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5f);

        [SerializeField]
        private Color _hoveredColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        [SerializeField]
        private Color _hoveredDisabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5f);

        [SerializeField]
        private Color _pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [SerializeField]
        private Color _pressedDisabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5f);

        [SerializeField]
        private bool _useScaleTransition;

        [SerializeField]
        private Transform _targetTransform;

        [SerializeField]
        private SimpleButtonColorBlendMode _scaleBlendMode = SimpleButtonColorBlendMode.Override;

        [SerializeField]
        private Vector3 _baseScale = Vector3.one;

        [Header("State scales")]
        [SerializeField]
        private Vector3 _normalScale = Vector3.one;

        [SerializeField]
        private Vector3 _normalDisabledScale = Vector3.one;

        [SerializeField]
        private Vector3 _hoveredScale = new Vector3(1.05f, 1.05f, 1f);

        [SerializeField]
        private Vector3 _hoveredDisabledScale = Vector3.one;

        [SerializeField]
        private Vector3 _pressedScale = new Vector3(0.95f, 0.95f, 1f);

        [SerializeField]
        private Vector3 _pressedDisabledScale = Vector3.one;

        private bool _isPointerDown;
        private bool _isHovered;

        public bool interactable
        {
            get => _interactable;
            set
            {
                if (_interactable == value)
                    return;
                _interactable = value;
                UpdateVisualState();
            }
        }

        public Button.ButtonClickedEvent onClick => _onClick;

        private void Awake()
        {
            if (_targetTransform == null)
                _targetTransform = transform;
        }

        private void OnEnable()
        {
            _isPointerDown = false;
            _isHovered = false;
            UpdateVisualState();
        }

        private void OnValidate()
        {
            if (_targetTransform == null)
                _targetTransform = transform;
            UpdateVisualState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
                return;

            _onClick?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
            UpdateVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
            UpdateVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            UpdateVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_useColorTransition && _targetGraphic != null)
            {
                Color stateColor = GetStateColor();
                _targetGraphic.color = ApplyColorBlend(_baseColor, stateColor);
            }

            if (_useScaleTransition && _targetTransform != null)
            {
                Vector3 stateScale = GetStateScale();
                _targetTransform.localScale = ApplyScaleBlend(_baseScale, stateScale);
            }
        }

        private Color GetStateColor()
        {
            if (_interactable)
            {
                if (_isPointerDown) return _pressedColor;
                if (_isHovered) return _hoveredColor;
                return _normalColor;
            }
            if (_isPointerDown) return _pressedDisabledColor;
            if (_isHovered) return _hoveredDisabledColor;
            return _normalDisabledColor;
        }

        private Vector3 GetStateScale()
        {
            if (_interactable)
            {
                if (_isPointerDown) return _pressedScale;
                if (_isHovered) return _hoveredScale;
                return _normalScale;
            }
            if (_isPointerDown) return _pressedDisabledScale;
            if (_isHovered) return _hoveredDisabledScale;
            return _normalDisabledScale;
        }

        private Color ApplyColorBlend(Color baseColor, Color stateColor)
        {
            switch (_colorBlendMode)
            {
                case SimpleButtonColorBlendMode.Additive:
                    return new Color(
                        Mathf.Clamp01(baseColor.r + stateColor.r),
                        Mathf.Clamp01(baseColor.g + stateColor.g),
                        Mathf.Clamp01(baseColor.b + stateColor.b),
                        Mathf.Clamp01(baseColor.a + stateColor.a));
                case SimpleButtonColorBlendMode.Multiply:
                    return new Color(
                        baseColor.r * stateColor.r,
                        baseColor.g * stateColor.g,
                        baseColor.b * stateColor.b,
                        baseColor.a * stateColor.a);
                default:
                    return stateColor;
            }
        }

        private Vector3 ApplyScaleBlend(Vector3 baseScale, Vector3 stateScale)
        {
            switch (_scaleBlendMode)
            {
                case SimpleButtonColorBlendMode.Additive:
                    return baseScale + stateScale;
                case SimpleButtonColorBlendMode.Multiply:
                    return new Vector3(
                        baseScale.x * stateScale.x,
                        baseScale.y * stateScale.y,
                        baseScale.z * stateScale.z);
                default:
                    return stateScale;
            }
        }
    }
}
