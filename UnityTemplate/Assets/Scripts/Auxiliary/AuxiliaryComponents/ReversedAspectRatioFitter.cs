using System;
using UnityEngine;

namespace AuxiliaryComponents
{

    [ExecuteAlways]
    public class ReversedAspectRatioFitter : MonoBehaviour
    {

        public enum Axis
        {
            WidthControlsHeight,
            HeightControlsWidth
        }

        [SerializeField]
        private Axis _axis;

        [SerializeField]
        private float _baseSize;
        [SerializeField]
        private float _baseAspectRatio;
        
        private RectTransform _rt;
        private DrivenRectTransformTracker _rtDriver;

        private void OnValidate()
        {
            _baseAspectRatio = Math.Max(0.001f, _baseAspectRatio);
        }

        protected void Awake()
        {
            _rt = (RectTransform)transform;
            UpdateRect();
        }

        private void OnEnable()
        {
            UpdateRect();
        }   

        private void Update()
        {
            UpdateRect();
        }

        private void Start()
        {
            UpdateRect();
        }

        private void OnTransformParentChanged()
        {
            UpdateRect();
        }
        
        private void OnBeforeTransformParentChanged()
        {
            UpdateRect();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }

        public void UpdateRect()
        {
            if (!enabled)
                return;
            
            if (!_rt)
            {
                if (this)
                    _rt = (RectTransform)transform;
            }
        
            if (!_rt)
                return;


            var initialRect = _rt.rect.size;
            var initialSize = _rt.sizeDelta;
            if (_axis == Axis.WidthControlsHeight)
            {
                var height = 1f / _baseAspectRatio * initialRect.x;  
                var targetRectY = _baseSize - height;
                var size = initialSize;
                if (_rt.anchorMax.y == _rt.anchorMin.y)
                    size.y = initialSize.y + (targetRectY - initialRect.y);
                else
                    size.y = initialSize.y - (targetRectY - initialRect.y);
                _rtDriver.Add(this, _rt, DrivenTransformProperties.SizeDeltaY);
                _rt.sizeDelta = size;
            }
            else
            {
                var width = 1f /_baseAspectRatio * initialRect.y;  
                var targetRectX = _baseSize - width;
                var size = initialSize;
                if (_rt.anchorMax.x == _rt.anchorMin.x)
                    size.x = initialSize.x + (targetRectX - initialRect.x);
                else
                    size.x = initialSize.x - (targetRectX - initialRect.x); 
                _rtDriver.Add(this, _rt, DrivenTransformProperties.SizeDeltaX);
                _rt.sizeDelta = size;
            }
            
        }

        private void OnDisable()
        {
            _rtDriver.Clear();
        }
    }
}