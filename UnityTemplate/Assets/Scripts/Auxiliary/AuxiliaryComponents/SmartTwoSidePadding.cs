using System;
using UnityEngine;

namespace AuxiliaryComponents
{
    
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class SmartTwoSidePadding : MonoBehaviour
    {
        [Flags]
        private enum PaddingType
        {
            None = 0,
            X = 0b1,
            Y = 0b10,
        }

        [SerializeField] private RectTransform _paddingLeft;
        [SerializeField] private RectTransform _paddingRight;
        [SerializeField] private PaddingType _paddingType = PaddingType.X;

        [SerializeField] private bool _checkSelfSize = false;

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
            var size = new Vector2();
            var pos = new Vector2();
            if ((_paddingType & PaddingType.X) > 0)
            {
                var padL = _paddingLeft == null ? 0f : -_paddingLeft.rect.width;
                var padR = _paddingRight == null ? 0f : -_paddingRight.rect.width;
                if (_checkSelfSize)
                {
                    padL -= _rectTransform.rect.width;
                }
                size.x = padL + padR;
                pos.x = (padR - padL) / 2f;
            }
            else
            {
                size.x = _rectTransform.sizeDelta.x;
                pos.x = _rectTransform.anchoredPosition.x;
            }
            
            if ((_paddingType & PaddingType.Y) > 0)
            {
                var padL = _paddingLeft == null ? 0f : _paddingLeft.rect.height;
                var padR = _paddingRight == null ? 0f : _paddingRight.rect.height;
                size.y = padL + padR;
                pos.y = (padR - padL) / 2f;
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