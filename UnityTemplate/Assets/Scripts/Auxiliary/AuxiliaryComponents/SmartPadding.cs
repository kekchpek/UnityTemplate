using System;
using UnityEngine;

namespace AuxiliaryComponents
{
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public class SmartPadding : MonoBehaviour
    {
        [Flags]
        private enum PaddingType
        {
            None = 0,
            X = 0b1,
            Y = 0b10,
        }

        [SerializeField] private RectTransform _padding;
        [SerializeField] private PaddingType _paddingType = PaddingType.X;

        private RectTransform _rectTransform;

        private void OnEnable()
        {
            _rectTransform = (RectTransform)transform;
            LateUpdate();
        }

        private void Awake()
        {
            OnEnable();
        }

        private void LateUpdate()
        {
            if (_padding == null)
                return;

            var size = new Vector2();
            var pos = new Vector2();
            if ((_paddingType & PaddingType.X) > 0)
            {
                size.x = -_padding.rect.width;
            }
            else
            {
                size.x = _rectTransform.sizeDelta.x;
                pos.x = _rectTransform.anchoredPosition.x;
            }
            
            if ((_paddingType & PaddingType.Y) > 0)
            {
                size.y = _padding.rect.height;
            }
            else
            {
                size.y = _rectTransform.sizeDelta.y;
                pos.y = _rectTransform.anchoredPosition.y;
            }
            _rectTransform.sizeDelta = size;
            
            _rectTransform.anchoredPosition = pos;
        }
    }
}