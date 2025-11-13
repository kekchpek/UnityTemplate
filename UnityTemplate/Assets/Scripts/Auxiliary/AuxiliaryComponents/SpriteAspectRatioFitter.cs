using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuxiliaryComponents
{
    [RequireComponent(typeof(AspectRatioFitter))]
    [RequireComponent(typeof(Image))]
    [ExecuteInEditMode]
    public class SpriteAspectRatioFitter : MonoBehaviour
    {
        private enum AxisToFit
        {
            X,
            Y,
            FitParent
        }

        private AspectRatioFitter _aspectRatioFitter;
        private Image _image;

        [SerializeField] private AxisToFit _axisToFit;

        private void OnEnable()
        {
            Start();
        }

        private void Start()
        {
            _aspectRatioFitter = GetComponent<AspectRatioFitter>();
            _image = GetComponent<Image>();
            Update();
        }

        private void Update()
        {
            var sprite = _image.sprite;
            if (!sprite)
            {
                _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.None;
            }
            else
            {
                _aspectRatioFitter.aspectMode = _axisToFit switch
                {
                    AxisToFit.X => AspectRatioFitter.AspectMode.WidthControlsHeight,
                    AxisToFit.Y => AspectRatioFitter.AspectMode.HeightControlsWidth,
                    AxisToFit.FitParent => AspectRatioFitter.AspectMode.FitInParent,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _aspectRatioFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            }
        }
    }
}