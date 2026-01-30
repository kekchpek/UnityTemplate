using UnityEngine;
using UnityEngine.UI;

namespace ArmorGuild.MVVM.Views.SkillTree.Component
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UILineRenderer : MaskableGraphic
    {
        [SerializeField]
        private Vector2[] _points = new Vector2[2];
        [SerializeField]
        private float _width = 1f;

        private Sprite _sprite;

        [SerializeField]
        private Vector2 _pointsScale = Vector2.one;
        
        [SerializeField]
        private Vector2 _pointsShift = Vector2.zero;

        public Vector2[] Points
        {
            get => _points;
            set
            {
                _points = value;
                SetVerticesDirty();
            }
        }

        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                SetVerticesDirty();
            }
        }

        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite != value)
                {
                    _sprite = value;
                    SetVerticesDirty();
                    SetMaterialDirty();
                }
            }
        }


        public Vector2 PointsScale
        {
            get => _pointsScale;
            set
            {
                _pointsScale = value;
                SetVerticesDirty();
            }
        }

        public Vector2 PointsShift
        {
            get => _pointsShift;
            set
            {
                _pointsShift = value;
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture
        {
            get
            {
                if (material != null && material.mainTexture != null)
                    return material.mainTexture;

                return _sprite == null ? s_WhiteTexture : _sprite.texture;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_points == null || _points.Length < 2)
                return;

            int vertexIndex = 0;
            for (int i = 0; i < _points.Length - 1; i++)
            {
                Vector2 start = _points[i] * _pointsScale + _pointsShift;
                Vector2 end = _points[i + 1] * _pointsScale + _pointsShift;
                Vector2 line = end - start;
                
                if (line.sqrMagnitude < 0.0001f)
                    continue;
                
                Vector2 perpendicular = new Vector2(-line.y, line.x).normalized * _width * 0.5f;

                UIVertex[] vertices = new UIVertex[4];
                for (int j = 0; j < 4; j++)
                {
                    vertices[j] = UIVertex.simpleVert;
                    vertices[j].color = color;
                }

                vertices[0].position = start + perpendicular;
                vertices[1].position = start - perpendicular;
                vertices[2].position = end + perpendicular;
                vertices[3].position = end - perpendicular;

                vertices[0].uv0 = new Vector2(0, 1);
                vertices[1].uv0 = new Vector2(0, 0);
                vertices[2].uv0 = new Vector2(1, 1);
                vertices[3].uv0 = new Vector2(1, 0);

                vh.AddVert(vertices[0]);
                vh.AddVert(vertices[1]);
                vh.AddVert(vertices[2]);
                vh.AddVert(vertices[3]);

                vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
                vh.AddTriangle(vertexIndex + 2, vertexIndex + 1, vertexIndex + 3);
                
                vertexIndex += 4;
            }
        }
    }
}
