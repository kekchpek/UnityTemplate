using UnityEngine;

namespace kekchpek
{
    public class MouseFollow : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private bool useSmoothing = true;
        
        // Cached references for optimization
        private Transform cachedTransform;
        private Vector3 lastMousePosition;
        private Vector3 targetPosition;
        
        void Start()
        {
            cachedTransform = transform;
            lastMousePosition = Input.mousePosition;
            UpdateTargetPosition();
        }

        void Update()
        {
            Vector3 currentMousePosition = Input.mousePosition;
            if (currentMousePosition != lastMousePosition)
            {
                lastMousePosition = currentMousePosition;
                UpdateTargetPosition();
            }
            
            if (useSmoothing)
            {
                cachedTransform.position = Vector3.Lerp(
                    cachedTransform.position, 
                    targetPosition, 
                    smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                cachedTransform.position = targetPosition;
            }
        }
        
        private void UpdateTargetPosition()
        {
            //TODO: Camera.main should not be used here. Some camera model should be created.
            //targetPosition = Camera.main.ScreenToWorldPoint(new Vector3(lastMousePosition.x, lastMousePosition.y, 0));
            targetPosition.z = 0;
        }
    }
}
