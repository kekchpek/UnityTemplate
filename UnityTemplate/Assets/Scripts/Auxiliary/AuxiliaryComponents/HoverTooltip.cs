using UnityEngine;
using UnityEngine.EventSystems;

namespace kekchpek.Auxiliary.Components
{
    public class HoverTooltip :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField]
        private GameObject _tooltip;

        private Canvas _rootCanvas;
        private RectTransform _tooltipRect;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

            if (_tooltip != null)
            {
                _tooltipRect = _tooltip.GetComponent<RectTransform>();
                _tooltip.SetActive(false);
            }
        }

        public void SetTooltip(GameObject tooltip)
        {
            _tooltip = tooltip;
            if (_tooltip != null)
            {
                _tooltipRect = _tooltip.GetComponent<RectTransform>();
                _tooltip.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }

            _tooltip.SetActive(true);
            ClampToCanvas();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltip == null)
            {
                return;
            }

            _tooltip.SetActive(false);
        }

        private void ClampToCanvas()
        {
            if (_tooltipRect == null || _rootCanvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            GetCanvasScreenBounds(_rootCanvas, out Vector2 screenMin, out Vector2 screenMax);

            Vector3[] tooltipCorners = new Vector3[4];
            _tooltipRect.GetWorldCorners(tooltipCorners);

            Vector3 tooltipMin = tooltipCorners[0];
            Vector3 tooltipMax = tooltipCorners[2];

            Vector3 offset = Vector3.zero;

            if (tooltipMin.x < screenMin.x)
            {
                offset.x = screenMin.x - tooltipMin.x;
            }
            else if (tooltipMax.x > screenMax.x)
            {
                offset.x = screenMax.x - tooltipMax.x;
            }

            if (tooltipMin.y < screenMin.y)
            {
                offset.y = screenMin.y - tooltipMin.y;
            }
            else if (tooltipMax.y > screenMax.y)
            {
                offset.y = screenMax.y - tooltipMax.y;
            }

            _tooltipRect.position += offset;
        }

        private static void GetCanvasScreenBounds(Canvas canvas, out Vector2 screenMin, out Vector2 screenMax)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenMin = Vector2.zero;
                screenMax = new Vector2(Screen.width, Screen.height);
            }
            else
            {
                Camera canvasCamera = canvas.worldCamera;
                if (canvasCamera != null)
                {
                    Vector3 bottomLeft = canvasCamera.ScreenToWorldPoint(Vector3.zero);
                    Vector3 topRight = canvasCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, canvas.planeDistance));
                    screenMin = bottomLeft;
                    screenMax = topRight;
                }
                else
                {
                    screenMin = Vector2.zero;
                    screenMax = new Vector2(Screen.width, Screen.height);
                }
            }
        }

        private void OnDisable()
        {
            if (_tooltip != null)
            {
                _tooltip.SetActive(false);
            }
        }
    }
}
