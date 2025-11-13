using AuxiliaryComponents.StaticUtils;
using UnityEngine;

namespace AuxiliaryComponents
{
    
    /// <summary>
    /// Make rect transform fit other rect transform(not necessarily a parent) by any axis.
    /// </summary>
    [ExecuteAlways]
    public class RectTransformFitter : MonoBehaviour
    {

        [SerializeField]
        private bool _fitX;
        [SerializeField]
        private bool _fitY;
        
        [SerializeField]
        private RectTransform _rectToFit;
        
        private RectTransform _rt;
        private DrivenRectTransformTracker _rtDriver;

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
            if (!_rectToFit)
                return;
            var position = _rt.position;
            var size = _rt.sizeDelta;
            _rtDriver.Clear();
            if (_fitX)
            {
                _rtDriver.Add(this, _rt, DrivenTransformProperties.SizeDeltaX);
                _rtDriver.Add(this, _rt, DrivenTransformProperties.AnchoredPositionX);
                position.x = _rectToFit.position.x;
                var lossyScale = _rt.lossyScale;
                size.x += (_rectToFit.rect.width * _rectToFit.lossyScale.x - _rt.rect.width * lossyScale.x) / lossyScale.x;
            }
            
            if (_fitY)
            {
                _rtDriver.Add(this, _rt, DrivenTransformProperties.SizeDeltaY);
                _rtDriver.Add(this, _rt, DrivenTransformProperties.AnchoredPositionY);
                var lossyScale = _rt.lossyScale;
                position.y = _rectToFit.position.y;
                size.y += (_rectToFit.rect.height * _rectToFit.lossyScale.y - _rt.rect.height * lossyScale.y) / lossyScale.y;
            }
            
            if (!AuxiliaryMath.Approximately(_rt.position, position, 0.01f))
                _rt.position = position;
            
            if (!AuxiliaryMath.Approximately(_rt.sizeDelta, size, 0.01f))
                _rt.sizeDelta = size;
        }

        private void OnDisable()
        {
            _rtDriver.Clear();
        }
    }
}