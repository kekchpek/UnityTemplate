using UnityEngine;

namespace kekchpek.Auxiliary.Components
{
    public class UiMouseAttacher : MonoBehaviour
    {
        private Canvas _rootCanvas;

        private Transform _transform;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas)
            {
                _rootCanvas = canvas.rootCanvas ?? canvas;
            }
            _transform = transform;
        }

        private void Update()
        {
            if (!_rootCanvas)
                return;
            var mousePosition = Input.mousePosition;
            var canvasPosition = _rootCanvas.worldCamera.ScreenToWorldPoint(mousePosition);
            _transform.position = new Vector3(canvasPosition.x, canvasPosition.y, _transform.position.z);
        }
    }
}