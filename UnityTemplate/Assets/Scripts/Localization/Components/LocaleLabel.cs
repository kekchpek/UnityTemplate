using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Zenject;

namespace kekchpek.Localization.Components
{
    
    [RequireComponent(typeof(TMP_Text))]
    public class LocaleLabel : MonoBehaviour, ILocaleLabel
    {

        [SerializeField] private string _localeKey;

        [SerializeField] private List<string> _formatArguments;

        private ILocalizationModel _localizationModel;

        private TMP_Text _text;

        private bool _inited;

        public string LocaleKey
        {
            get => _localeKey;
            set
            {
                _localeKey = value;
                if (_localizationModel == null) // in case of changes in editor
                {
                    return;
                }
                UpdateString();
            }
        }

        public void SetFormattingArgs(IEnumerable<string> args)
        {
            _formatArguments = args.ToList();
            UpdateString();
        }

        [Inject]
        public void Construct(ILocalizationModel localizationModel)
        {
            _localizationModel = localizationModel;
        }
        
        private void Awake()
        {
            if (_inited)
            {
                return;
            }

            _inited = true;
            _text = GetComponent<TMP_Text>();
            _localizationModel.OnLocaleChanged += UpdateString;
            UpdateString();
        }

        private void UpdateString()
        {
            Awake();
            if (string.IsNullOrEmpty(_localeKey))
            {
                _text.text = string.Empty;
                return;
            }

            if (_formatArguments != null && _formatArguments.Any())
            {
                _text.text = string.Format(
                    _localizationModel.GetLocalizedString(_localeKey),
                    _formatArguments.Cast<object>().ToArray());
            }
            else
            {
                _text.text = _localizationModel.GetLocalizedString(_localeKey);
            }
        }

        private void OnDestroy()
        {
            _localizationModel.OnLocaleChanged -= UpdateString;
        }
    }
}