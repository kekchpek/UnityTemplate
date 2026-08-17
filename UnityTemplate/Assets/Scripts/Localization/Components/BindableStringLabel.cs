using AsyncReactAwait.Bindable;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class BindableStringLabel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        private IBindable<string> _boundText;

        private void Awake()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }
        }

        public void SetText(IBindable<string> text)
        {
            if (_boundText != null)
            {
                _boundText.Unbind(UpdateText);
            }

            _boundText = text;

            if (_boundText != null)
            {
                _boundText.Bind(UpdateText);
            }
            else
            {
                UpdateText(string.Empty);
            }
        }

        public void Clear()
        {
            SetText(null);
        }

        private void UpdateText(string value)
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            _text.text = value ?? string.Empty;
        }

        private void OnDestroy()
        {
            if (_boundText != null)
            {
                _boundText.Unbind(UpdateText);
            }
        }
    }
}
