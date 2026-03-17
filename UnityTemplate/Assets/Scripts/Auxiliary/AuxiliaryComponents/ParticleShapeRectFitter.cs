using UnityEngine;

namespace AuxiliaryComponents
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class ParticleShapeRectFitter : MonoBehaviour
    {


        public float ParticlesDensity
        {
            get => _particlesDensity;
            set
            {
                _particlesDensity = value;
                UpdateShapeScale();
            }
        }

        public float BurstsDensity
        {
            get => _burstsDensity;
            set
            {
                _burstsDensity = value;
                UpdateShapeScale();
            }
        }

        [SerializeField]
        private ParticleSystem _particleSystem;

        [SerializeField]
        private float _particlesDensity = 1f;

        [SerializeField]
        private float _burstsDensity = 1f;

        private RectTransform _rectTransform;
        private bool _isInitialized;

        private void Awake()
        {
            CacheComponents();
            InitializeBaseDataIfNeeded();
            UpdateShapeScale();
        }

        private void OnEnable()
        {
            CacheComponents();
            InitializeBaseDataIfNeeded();
            UpdateShapeScale();
        }

        private void OnValidate()
        {
            CacheComponents();
            InitializeBaseDataIfNeeded();
            UpdateShapeScale();
        }

        private void Update()
        {
            InitializeBaseDataIfNeeded();
            UpdateShapeScale();
        }

        private void OnRectTransformDimensionsChange()
        {
            UpdateShapeScale();
        }

        private void CacheComponents()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        private void InitializeBaseDataIfNeeded()
        {
            if (_isInitialized || _particleSystem == null || _rectTransform == null)
            {
                return;
            }

            var rectSize = _rectTransform.rect.size;
            if (rectSize.x <= 0f || rectSize.y <= 0f)
            {
                return;
            }
            _isInitialized = true;
        }

        private void UpdateShapeScale()
        {
            if (_particleSystem == null || _rectTransform == null || !_isInitialized)
            {
                return;
            }

            var currentRectSize = _rectTransform.rect.size;
            if (currentRectSize.x <= 0f || currentRectSize.y <= 0f)
            {
                return;
            }

            var shape = _particleSystem.shape;
            shape.scale = new Vector3(
                currentRectSize.x,
                currentRectSize.y,
                shape.scale.z);
            var emission = _particleSystem.emission;
            var area = currentRectSize.x * currentRectSize.y;
            UpdateParticlesByDensity(emission, area);
            UpdateBurstsByDensity(emission, area);
        }

        private void UpdateParticlesByDensity(ParticleSystem.EmissionModule emission, float area)
        {
            emission.rateOverTime = area * _particlesDensity;
        }

        private void UpdateBurstsByDensity(ParticleSystem.EmissionModule emission, float area)
        {
            int burstCount = emission.burstCount;
            if (burstCount <= 0)
            {
                return;
            }

            var targetCount = (short)Mathf.Clamp(
                Mathf.RoundToInt(area * _burstsDensity),
                0,
                short.MaxValue);

            var bursts = new ParticleSystem.Burst[burstCount];
            emission.GetBursts(bursts);
            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i].minCount = targetCount;
                bursts[i].maxCount = targetCount;
            }

            emission.SetBursts(bursts, bursts.Length);
        }
    }
}
