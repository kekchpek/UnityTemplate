using AuxiliaryComponents.StaticUtils;
using TMPro;
using UnityEngine;

namespace AuxiliaryComponents
{
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(RectTransform))]
    public class TextAutoScaler : MonoBehaviour
    {
        
        private enum Border
        {
            Left,
            Right,
            Center,
            None,
        }

        [SerializeField] private bool _executeInEditor;
        [SerializeField] private Border _border;
        
        private TMP_Text _text;
        private RectTransform _rectTransform;
        
        private void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            _rectTransform = GetComponent<RectTransform>();
            Update();
        }

        private void Start()
        {
            OnEnable();
        }

        private void Update()
        {
            _text.UnregisterDirtyLayoutCallback(Update);
            if (!_executeInEditor)
            {
                return;
            }

            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000f);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rectTransform.rect.height);
            _text.ForceMeshUpdate();
            
            var width = TextUtils.TextWidthApproximation(_text.text, _text.font, _text.fontSize, _text.fontStyle);
            var size = _rectTransform.sizeDelta;
            size.x = width;
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);

            if (_border != Border.None)
            {
                if (_border != Border.Center)
                {
                    var pos = _rectTransform.anchoredPosition;
                    pos.x = width / 2f;
                    if (_border == Border.Right)
                    {
                        pos.x = -pos.x;
                    }

                    _rectTransform.anchoredPosition = pos;
                }
                else
                {
                    _rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            _text.RegisterDirtyLayoutCallback(Update);
        }

        private void OnDestroy()
        {
            _text.UnregisterDirtyLayoutCallback(Update);
        }
    }
}