using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuxiliaryComponents
{
    public class GradientImage : Image
    {
        [Serializable]
        public enum Direction
        {
            Horizontal,
            Vertical,
        }

        [SerializeField]
        private Color _gradientColor = Color.black;

        [SerializeField]
        private Direction _direction = Direction.Vertical;

        public Color GradientColor
        {
            get => _gradientColor;
            set
            {
                if (_gradientColor == value)
                    return;
                _gradientColor = value;
                SetVerticesDirty();
            }
        }

        public Direction GradientDirection
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;
                _direction = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            var rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            UIVertex vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = _direction == Direction.Horizontal
                    ? Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x)
                    : Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                vertex.color = Color.Lerp(color, _gradientColor, t);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
