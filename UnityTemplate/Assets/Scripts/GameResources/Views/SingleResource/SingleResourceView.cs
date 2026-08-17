using System;
using kekchpek.MVVM.Models.GameResources.Static;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityMVVM;

namespace kekchpek.MVVM.Models.GameResources.Views.SingleResource
{
    public class SingleResourceView : ViewBehaviour<ISingleResourceViewModel>
    {

        private struct ComingValue
        {
            public bool isActive;
            public float value;
        }

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private string _resourceId;

        private int _comingValueIndex = 0;

        private ComingValue[] _comingValues;

        /// <summary>
        /// Reserves an amount that is subtracted from the displayed value until released.
        /// Only supported for resources exposing a numeric value (see
        /// <see cref="ISingleResourceViewModel.HasRawValue"/>).
        /// </summary>
        /// <returns>Id to pass to <see cref="RemoveComingValue"/>.</returns>
        public int AddComingValue(float value)
        {
            if (_comingValues == null)
            {
                _comingValues = new ComingValue[1];
            }
            var indexToPlace = _comingValueIndex;
            do {
                if (_comingValues[indexToPlace].isActive)
                {
                    indexToPlace = (indexToPlace + 1) % _comingValues.Length;
                }
                else
                {
                    _comingValues[indexToPlace].isActive = true;
                    _comingValues[indexToPlace].value = value;
                    _comingValueIndex = (indexToPlace + 1) % _comingValues.Length;
                    UpdateText();
                    return indexToPlace;
                }
            } while (indexToPlace != _comingValueIndex);
            var newComingValues = new ComingValue[_comingValues.Length * 2];
            var oldLength = _comingValues.Length;
            Array.Copy(_comingValues, newComingValues, oldLength);
            _comingValues = newComingValues;
            _comingValues[oldLength].isActive = true;
            _comingValues[oldLength].value = value;
            _comingValueIndex = (oldLength + 1) % newComingValues.Length;
            UpdateText();
            return oldLength;
        }

        public void RemoveComingValue(int valueId)
        {
            if (_comingValues == null)
            {
                Debug.LogError("Coming values are not initialized");
                return;
            }
            if (valueId < 0 || valueId >= _comingValues.Length)
            {
                Debug.LogError($"Invalid value id: {valueId}");
                return;
            }
            if (!_comingValues[valueId].isActive)
            {
                Debug.LogError($"Value {valueId} is not active");
                return;
            }
            _comingValues[valueId].isActive = false;
            UpdateText();
        }

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();
            if (_icon)
                ViewModel.EnableIconLoading();
            if (!string.IsNullOrEmpty(_resourceId))
                ViewModel!.SetResource(_resourceId);
            SmartBind(ViewModel!.Icon, x =>
            {
                if (_icon)
                    _icon.sprite = x;
            });
            SmartBind(ViewModel!.Value, _ => UpdateText());
            SmartBind(ViewModel!.RawValue, _ => UpdateText());
        }

        private void UpdateText()
        {
            if (!_text)
                return;

            var comingTotal = 0f;
            if (_comingValues != null)
            {
                for (int i = 0; i < _comingValues.Length; i++)
                {
                    if (_comingValues[i].isActive)
                    {
                        comingTotal += _comingValues[i].value;
                    }
                }
            }

            // Large resources have no numeric representation, and with nothing reserved the
            // view model's already formatted value is authoritative.
            if (comingTotal == 0f || !ViewModel!.HasRawValue)
            {
                _text.text = ViewModel!.Value.Value;
                return;
            }

            _text.text = ResourceFormatting.FormatNumber(ViewModel!.RawValue.Value - comingTotal);
        }
    }
}
