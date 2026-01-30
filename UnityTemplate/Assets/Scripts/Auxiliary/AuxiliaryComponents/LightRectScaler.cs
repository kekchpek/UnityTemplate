using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace kekchpek.Auxiliary.Components
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Light2D))]
    [ExecuteInEditMode]
    public class LightRectScaler : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Light2D _light;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _light = GetComponent<Light2D>();
            UpdateLightRadius();
        }

        private void Update()
        {
            UpdateLightRadius();
        }

        private void UpdateLightRadius()
        {
            if (_rectTransform == null || _light == null)
            {
                return;
            }

            var radiusesRatio = _light.pointLightInnerRadius / _light.pointLightOuterRadius;

            var corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);
            
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);
            
            float radius = Mathf.Min(worldWidth, worldHeight) / 2f;
            _light.pointLightOuterRadius = radius;
            _light.pointLightInnerRadius = radius * radiusesRatio;
        }
    }
}