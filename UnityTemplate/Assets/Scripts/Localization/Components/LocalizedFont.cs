using TMPro;
using UnityEngine;
using Zenject;

namespace kekchpek.Localization.Components
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedFont : MonoBehaviour
    {
        private ILocalizationModel _localizationModel;

        private TMP_FontAsset _font;

        private Material _fontMaterial;

        private TMP_Text _text;

        private bool _inited;

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
            _localizationModel.CurrentLocale.Bind(OnLocaleChanged);
        }

        private void OnLocaleChanged(string localeKey)
        {
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
            }
        }

        private void OnDestroy()
        {
            if (_localizationModel == null)
            {
                return;
            }

            _localizationModel.CurrentLocale.Unbind(OnLocaleChanged);
            _localizationModel.IsSpecialFontsLoaded.Unbind(OnSpecialFontsLoaded);
        }
    }
}
