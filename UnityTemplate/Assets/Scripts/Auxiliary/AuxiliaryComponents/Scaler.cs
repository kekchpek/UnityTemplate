using UnityEngine;

namespace AuxiliaryComponents
{
    public class Scaler : MonoBehaviour
    {
        private Transform _transform;
        private Vector3 _initialScale;

        [SerializeField]
        private float _scale = 1f;

        public float Scale
        {
            get => _scale;
            set => _scale = value;
        }

        private void Awake()
        {
            _transform = transform;
            ResetScale();
        }

        public void ResetScale()
        {
            _initialScale = _transform.localScale;
        }

        private void Update()
        {
            _transform.localScale = Scale * _initialScale;
        }
    }
}