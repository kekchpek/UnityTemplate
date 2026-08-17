using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using Zenject;

namespace kekchpek.Localization.Components
{
    
    [RequireComponent(typeof(TMP_Text))]
    public class LocaleLabel : MonoBehaviour, ILocaleLabel
    {

        [SerializeField] private string _localeKey;

        [SerializeField] private object[] _formatArguments;

        private ILocalizationModel _localizationModel;

        private TMP_FontAsset _font;

        private Material _fontMaterial;

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
                if (_localizationModel.IsInitialized.Value && _inited)
                {
                    OnLocaleChanged(_localizationModel.CurrentLocale.Value);
                } // otherwise it will be update on initialization
            }
        }

        public void SetFormattingArgs(IEnumerable<string> args)
        {
            var list = UnityEngine.Pool.ListPool<object>.Get();
            foreach (object arg in args)
            {
                list.Add(arg);
            }
            _formatArguments = new object[list.Count];
            list.CopyTo(_formatArguments, 0);
            UnityEngine.Pool.ListPool<object>.Release(list);
            if (_localizationModel.IsInitialized.Value && _inited)
            {
                OnLocaleChanged(_localizationModel.CurrentLocale.Value);
            } // otherwise it will be update on initialization
        }

        public void SetFormattingArgs(ReadOnlySpan<string> args)
        {
            _formatArguments = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                _formatArguments[i] = args[i];
            }
            if (_localizationModel.IsInitialized.Value && _inited)
            {
                OnLocaleChanged(_localizationModel.CurrentLocale.Value);
            } // otherwise it will be update on initialization
        }

        [Inject]
        public void Construct(
            ILocalizationModel localizationModel)
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
            _font = _text.font;
            _fontMaterial = _text.fontSharedMaterial;
            _localizationModel.CurrentLocale.Bind(OnLocaleChanged, false);
            _localizationModel.IsInitialized.Bind(OnInitializedChanged);
        }

        private void OnInitializedChanged(bool isInitialized)
        {
            if (isInitialized)
            {
                OnLocaleChanged(_localizationModel.CurrentLocale.Value);
            }
        }

        private void OnLocaleChanged(string localeKey)
        {
            if (localeKey == null)
            {
                _text.text = string.Empty;
                return;
            }
            _localizationModel.IsSpecialFontsLoaded.Unbind(OnSpecialFontsLoaded);
            if (_localizationModel.HasSpecialFont(localeKey, _font) ||
                _localizationModel.HasSpecialFontMaterial(localeKey, _fontMaterial)) 
            {
                _localizationModel.IsSpecialFontsLoaded.Bind(OnSpecialFontsLoaded);
            }
            else
            {
                _text.font = _font;
                if (_localizationModel.HasSpecialFontMaterial(localeKey, _fontMaterial))
                {
                    _text.fontSharedMaterial = _fontMaterial;
                }
                UpdateString();
            }
        }

        private void OnSpecialFontsLoaded(bool isLoaded)
        {
            if (isLoaded)
            {
                if (_localizationModel.HasSpecialFontMaterial(_localizationModel.CurrentLocale.Value, _fontMaterial))
                {
                    _text.fontSharedMaterial = _localizationModel.GetSpecialFontMaterial(_fontMaterial);
                }
                if (_localizationModel.HasSpecialFont(_localizationModel.CurrentLocale.Value, _font))
                {
                    _text.font = _localizationModel.GetSpecialFont(_font);
                }
                UpdateString();
            }
        }

        private void UpdateString()
        {
            Awake();
            if (string.IsNullOrEmpty(_localeKey))
            {
                _text.text = string.Empty;
                return;
            }

            if (_formatArguments != null && _formatArguments.Length > 0)
            {
                _text.text = string.Format(
                    _localizationModel.GetLocalizedString(_localeKey),
                    _formatArguments);
            }
            else
            {
                _text.text = _localizationModel.GetLocalizedString(_localeKey);
            }
        }

        private void OnDestroy()
        {
            _localizationModel.CurrentLocale.Unbind(UpdateString);
        }
    }
}