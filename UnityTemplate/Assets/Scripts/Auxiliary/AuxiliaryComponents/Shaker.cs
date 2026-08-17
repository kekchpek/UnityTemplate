using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using kekchpek.Auxiliary.Collections;
using UnityEngine;

namespace AuxiliaryComponents
{
    public class Shaker : MonoBehaviour
    {

        private static int ShakeIndex = 0;

        private Transform _target;

        [SerializeField]
        private List<Transform> _stabilizationTargets = new();

        [SerializeField]
        private bool _enabled = true;

        private readonly List<Vector3> _stabilizationOriginalPositions = new();

        private Vector3 _originalPosition;

        private Dictionary<int, Vector3> _shakeOffsets = new(3);

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private void Start()
        {
            _target = Camera.main.transform;
            _originalPosition = _target.position;
            foreach (var stabilizationTarget in _stabilizationTargets)
            {
                _stabilizationOriginalPositions.Add(stabilizationTarget.position);
            }
        }

        public void Shake(float duration, float strength, int vibrato = 10, float randomness = 90, bool snapping = false, bool fadeOut = true, ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Full, bool ignoreEnabled = false)
        {
            if (!Enabled && !ignoreEnabled)
            {
                return;
            }
            strength /= 150f;
            int shakeIndex = ShakeIndex++;
            _shakeOffsets.Add(shakeIndex, default);
            var tween = DOTween.Shake(() => _shakeOffsets[shakeIndex], delegate (Vector3 x)
            {
                _shakeOffsets[shakeIndex] = x;
                ApplyShakeOffsets();
            }, duration, strength, vibrato, randomness, ignoreZAxis: true, fadeOut, randomnessMode).SetTarget(_target).SetSpecialStartupMode(SpecialStartupMode.SetShake)
                .SetOptions(snapping);
            tween.OnComplete(() =>
            {
                _shakeOffsets.Remove(shakeIndex);
                ApplyShakeOffsets();
            });
        }

        private void ApplyShakeOffsets()
        {
            var shakePosition = default(Vector3);
            foreach (var shakeOffset in _shakeOffsets)
            {
                shakePosition += shakeOffset.Value;
            }
            _target.position = _originalPosition + shakePosition;
            for (int i = 0; i < _stabilizationTargets.Count; i++)
            {
                Transform stabilizationTarget = _stabilizationTargets[i];
                stabilizationTarget.position = _stabilizationOriginalPositions[i] - shakePosition;
            }
        }


    }
}