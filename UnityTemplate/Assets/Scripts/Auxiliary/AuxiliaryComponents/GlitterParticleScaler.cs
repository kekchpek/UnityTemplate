using UnityEngine;

namespace AuxiliaryComponents
{
    [ExecuteInEditMode]
    public class GlitterParticleScaler : MonoBehaviour
    {
        /// <summary>
        /// Resolution at which particles look correct
        /// </summary>
        private static readonly Vector2 ParticleTargetResolution = new Vector2(1080f, 2345f);

        void Start()
        {
            ScaleParticles();
        }

#if UNITY_EDITOR

        private void Update()
        {
            ScaleParticles();
        }

#endif

        private void ScaleParticles()
        {
            float bakeRatioVertical = ParticleTargetResolution.y / ParticleTargetResolution.x;
            float screenRatioVertical = (float)Screen.height / (float)Screen.width;
            float scaleRatio = bakeRatioVertical / screenRatioVertical;

            transform.localScale = new Vector3(scaleRatio, scaleRatio, scaleRatio);
        }

    }
}
