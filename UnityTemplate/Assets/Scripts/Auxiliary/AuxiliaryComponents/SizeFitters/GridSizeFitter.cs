using UnityEngine;
using UnityEngine.UI;

namespace kekchpek.AuxiliaryComponents.SizeFitters
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class GridSizeFitter : MonoBehaviour, ILayoutSelfController
    {
        public enum FitMode
        {
            Unconstrained,
            MinSize,
            PreferredSize,
        }

        [SerializeField]
        private FitMode _horizontalFit = FitMode.Unconstrained;

        [SerializeField]
        private FitMode _verticalFit = FitMode.Unconstrained;

        private GridLayoutGroup _grid;
        private RectTransform _rectTransform;
        private DrivenRectTransformTracker _tracker;

        private void Awake()
        {
            Cache();
        }

        private void OnEnable()
        {
            Cache();
            SetDirty();
        }

        private void OnDisable()
        {
            _tracker.Clear();
            if (_rectTransform != null)
                LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Cache();
            SetDirty();
        }
#endif

        private void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        public void SetLayoutHorizontal()
        {
            _tracker.Clear();
            ApplyAxisFit(RectTransform.Axis.Horizontal, _horizontalFit);
        }

        public void SetLayoutVertical()
        {
            ApplyAxisFit(RectTransform.Axis.Vertical, _verticalFit);
        }

        private void Cache()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void SetDirty()
        {
            if (!isActiveAndEnabled || _rectTransform == null)
                return;

            LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
        }

        private void ApplyAxisFit(RectTransform.Axis axis, FitMode fitMode)
        {
            if (fitMode == FitMode.Unconstrained || _grid == null || _rectTransform == null)
                return;

            var drivenProperty = axis == RectTransform.Axis.Horizontal
                ? DrivenTransformProperties.SizeDeltaX
                : DrivenTransformProperties.SizeDeltaY;

            _tracker.Add(this, _rectTransform, drivenProperty);

            float size = axis == RectTransform.Axis.Horizontal
                ? CalculateHorizontalSize(fitMode)
                : CalculateVerticalSize();

            _rectTransform.SetSizeWithCurrentAnchors(axis, size);
        }

        private float CalculateHorizontalSize(FitMode fitMode)
        {
            int cellCount = GetActiveChildCount();
            int columns = GetColumnCount(cellCount, fitMode);
            var padding = _grid.padding;
            var cellSize = _grid.cellSize;
            var spacing = _grid.spacing;

            return padding.horizontal + (cellSize.x + spacing.x) * columns - spacing.x;
        }

        private float CalculateVerticalSize()
        {
            int cellCount = GetActiveChildCount();
            int rows = GetRowCount(cellCount);
            var padding = _grid.padding;
            var cellSize = _grid.cellSize;
            var spacing = _grid.spacing;

            return padding.vertical + (cellSize.y + spacing.y) * rows - spacing.y;
        }

        private int GetColumnCount(int cellCount, FitMode fitMode)
        {
            switch (_grid.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    return _grid.constraintCount;
                case GridLayoutGroup.Constraint.FixedRowCount:
                    return Mathf.CeilToInt(cellCount / (float)_grid.constraintCount - 0.001f);
                default:
                    return fitMode == FitMode.MinSize
                        ? 1
                        : Mathf.CeilToInt(Mathf.Sqrt(cellCount));
            }
        }

        private int GetRowCount(int cellCount)
        {
            switch (_grid.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    return Mathf.CeilToInt(cellCount / (float)_grid.constraintCount - 0.001f);
                case GridLayoutGroup.Constraint.FixedRowCount:
                    return _grid.constraintCount;
                default:
                    float width = _rectTransform.rect.width;
                    int cellCountX = Mathf.Max(
                        1,
                        Mathf.FloorToInt(
                            (width - _grid.padding.horizontal + _grid.spacing.x + 0.001f)
                            / (_grid.cellSize.x + _grid.spacing.x)));

                    return Mathf.CeilToInt(cellCount / (float)cellCountX);
            }
        }

        private int GetActiveChildCount()
        {
            int count = 0;

            for (int i = 0; i < _rectTransform.childCount; i++)
            {
                var child = _rectTransform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;

                var layoutElement = child.GetComponent<LayoutElement>();
                if (layoutElement != null && layoutElement.ignoreLayout)
                    continue;

                count++;
            }

            return count;
        }
    }
}
