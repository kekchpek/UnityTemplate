using UnityEngine;

namespace AuxiliaryComponents
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class UiRelativePositionSetter : MonoBehaviour
    {

        private enum SetMode
        {
            Screen,
            ParentRect,
        }
        
        private RectTransform _rectTransform;

        [SerializeField] private float _y;
        [SerializeField] private float _x;

        [SerializeField] private bool _controlPositionInEditor;
        [SerializeField] private SetMode _mode = SetMode.Screen;
        
        private void OnEnable()
        {
            Awake();
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void Update()
        {
            if (Application.isPlaying || _controlPositionInEditor)
            {
                switch (_mode)
                {
                    case SetMode.Screen:
                        _rectTransform.anchoredPosition = new Vector2(
                            Screen.width * _x,
                            Screen.height * _y);
                        break;
                    case SetMode.ParentRect:
                        RectTransform parent = (RectTransform)transform.parent;
                        var rect = parent.rect;
                        _rectTransform.anchoredPosition = new Vector2(
                            rect.width * _x,
                            rect.width * _y);
                        break;
                }
            }
            else if (Application.isEditor)
            {
                var anchoredPosition = _rectTransform.anchoredPosition;
                switch (_mode)
                {
                    case SetMode.Screen:
                        _x = anchoredPosition.x / Screen.width;
                        _y = anchoredPosition.y / Screen.height;
                        break;
                    case SetMode.ParentRect:
                        RectTransform parent = (RectTransform)transform.parent;
                        var rect = parent.rect;
                        _x = anchoredPosition.x / rect.width;
                        _y = anchoredPosition.y / rect.height;
                        break;
                }
            }
        }
    }
}
