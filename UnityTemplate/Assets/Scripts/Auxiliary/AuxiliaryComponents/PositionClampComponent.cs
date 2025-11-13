using System;
using AsyncReactAwait.Bindable;
using UnityEngine;

namespace AuxiliaryComponents
{
    public class PositionClampComponent : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _clampArea;

        [SerializeField]
        private bool _clampMinX;
        [SerializeField]
        private bool _clampMinY;

        [SerializeField]
        private bool _clampMaxX;
        [SerializeField]
        private bool _clampMaxY;

        private RectTransform _transform;

        private readonly Mutable<bool> _isClampingTop = new();
        public IBindable<bool> IsClampingTop => _isClampingTop;

        private readonly Mutable<bool> _isClampingBottom = new();
        public IBindable<bool> IsClampingBottom => _isClampingBottom;

        public RectTransform ClampArea
        {
            get => _clampArea;
            set => _clampArea = value;
        }

        private void Awake()
        {
            _transform = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            var corners = new Vector3[4];
            _clampArea.GetWorldCorners(corners);
            var minY = corners[0].y;
            var minX = corners[0].x;
            var maxY = corners[2].y;
            var maxX = corners[2].x;
            var pos = _transform.parent.position;
            _isClampingBottom.Value = pos.y < minY;
            _isClampingTop.Value = pos.y > maxY;
            pos = new Vector3(
                Math.Clamp(pos.x, _clampMinX ? minX : float.NegativeInfinity, _clampMaxX ? maxX : float.PositiveInfinity),
                Math.Clamp(pos.y, _clampMinY ? minY : float.NegativeInfinity, _clampMaxY ? maxY : float.PositiveInfinity),
                pos.z
            );
            _transform.position = pos;
        }

        private void OnDisable()
        {
            _transform.localPosition = Vector3.zero;
        }
    }
}