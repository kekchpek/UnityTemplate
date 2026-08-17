using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AsyncReactAwait.Bindable;
using kekchpek.GameSaves;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization
{
    public class LocalizationModel : ILocalizationMutableModel
    {

        private const string MissingString = "<MISSING_STRING>";
        
        public event Action OnLocaleChanged;
        public event Action NewLocaleReadyToBeDisplayed;

        private bool _spicialFontsSet;
        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _localizationData;
        private readonly Mutable<bool> _isInitialized = new(false);
        private readonly Dictionary<string, ReadOnlyDictionary<TMP_FontAsset, string>> _fontsMap = new();
        private readonly Dictionary<string, ReadOnlyDictionary<Material, string>> _fontMaterialsMap = new();
        private readonly Mutable<bool> _isSpecialFontsLoaded = new(false);
        private readonly IGameSaveManager _gameSaveManager;
        private IMutable<string> _currentLocale;
        private string _defaultLocale;
        private bool _newLocaleWaitReadyToBeDisplayed;
        private Dictionary<TMP_FontAsset, TMP_FontAsset> _specialFonts;
        private Dictionary<Material, Material> _specialFontMaterials;
        public IBindable<string> CurrentLocale => _currentLocale;
        public IBindable<bool> IsSpecialFontsLoaded => _isSpecialFontsLoaded;
        public IBindable<bool> IsInitialized => _isInitialized;

        public LocalizationModel(IGameSaveManager gameSaveManager)
        {
            _gameSaveManager = gameSaveManager;
            _gameSaveManager.IsInitialized.Bind(OnGameSaveManagerInitialized);
        }

        public TMP_FontAsset GetSpecialFont(TMP_FontAsset font)
        {
            if (!_isSpecialFontsLoaded.Value)
            {
                Debug.LogError("Special fonts not loaded!");
                return font;
            }
            if (_specialFonts.TryGetValue(font, out var specialFont))
            {
                return specialFont;
            }

            return font;
        }

        public Material GetSpecialFontMaterial(Material fontMaterial)
        {
            if (!_isSpecialFontsLoaded.Value)
            {
                Debug.LogError("Special fonts not loaded!");
                return fontMaterial;
            }

            if (_specialFontMaterials.TryGetValue(fontMaterial, out var specialFontMaterial))
            {
                return specialFontMaterial;
            }

            return fontMaterial;
        }

        public void SetSpecialFonts(
            Dictionary<TMP_FontAsset, TMP_FontAsset> fonts, 
            Dictionary<Material, Material> fontMaterials)
        {
            _specialFonts = fonts;
            _specialFontMaterials = fontMaterials;
            _isSpecialFontsLoaded.Value = true;
            if (_newLocaleWaitReadyToBeDisplayed) 
            {
                _newLocaleWaitReadyToBeDisplayed = false;
            }
        }

        public void SetupFontData(FontsConfig fontsConfig) {
            foreach (var fontLocaleMapping in fontsConfig.FontLocaleMappings)
            {
                var dict = new Dictionary<TMP_FontAsset, string>();
                foreach (var fontMapEntry in fontLocaleMapping.FontMapEntries)
                {
                    dict.Add(fontMapEntry.Font, fontMapEntry.LocaleFontPath);
                }
                _fontsMap.Add(fontLocaleMapping.Locale, new ReadOnlyDictionary<TMP_FontAsset, string>(dict));
                var fontMaterialsDict = new Dictionary<Material, string>();
                foreach (var fontMaterialMapEntry in fontLocaleMapping.FontMaterialMapEntries)
                {
                    fontMaterialsDict.Add(fontMaterialMapEntry.Material, fontMaterialMapEntry.LocaleMaterialPath);
                }
                _fontMaterialsMap.Add(fontLocaleMapping.Locale, new ReadOnlyDictionary<Material, string>(fontMaterialsDict));
            }
            _spicialFontsSet = true;
            UpdateInitialized();
        }

        public void ClearSpecialFonts()
        {
            _specialFonts = null;
            _specialFontMaterials = null;
            _isSpecialFontsLoaded.Value = false;
        }

        private void OnGameSaveManagerInitialized(bool isInitialized)
        {
            if (isInitialized)
            {
                _currentLocale = _gameSaveManager.SettingsDataProvider.DeserializeAndCaptureCustomValue("Localization/CurrentLocale", () => (string)null);
                _newLocaleWaitReadyToBeDisplayed = true;
                UpdateInitialized();
            }
        }

        public string GetLocalizedString(string key)
        {
            if (_localizationData == null)
            {
                Debug.LogError("Localization data not set!");
                return MissingString;
            }

            string stringToReturn = null;
            if (_localizationData.TryGetValue(_currentLocale.Value, out var localeData))
            {
                if (localeData.TryGetValue(key, out var localizedString))
                {
                    stringToReturn = localizedString;
                }
                else
                {
                    Debug.LogWarning($"Can not find string for locale key = {key}; locale = {_currentLocale}!");
                }
            }
            else
            {
                Debug.LogWarning($"Locale data can not be found for {_currentLocale}!");
            }

            if (stringToReturn == null)
            {
                if (_localizationData.TryGetValue(_defaultLocale, out var defaultLocaleData))
                {
                    if (defaultLocaleData.TryGetValue(key, out var localizedString))
                    {
                        stringToReturn = localizedString;
                    }
                    else
                    {
                        Debug.LogError($"Can not find string for locale key = {key}; default locale = {_defaultLocale}!");
                    }
                }
                else
                {
                    Debug.LogError($"Locale data can not be found for default locale = {_defaultLocale}!");
                }
            }

            if (stringToReturn == null)
            {
                Debug.LogError($"Neither locale nor default locale data can not be found for {key}!");
                return MissingString;
            }

            return stringToReturn;
        }

        public void SetLocale(string localeKey)
        {
            if (string.IsNullOrEmpty(localeKey))
            {
                Debug.LogError("Locale key is null or empty!");
                return;
            }
            _currentLocale.Value = localeKey;
            _newLocaleWaitReadyToBeDisplayed = false;
            UpdateInitialized();
        }

        public LocalizedString ConstructLocalizedString(Func<ILocalizationModel, string> stringBuilder) 
        {
            return new LocalizedString(stringBuilder, this);
        }

        public LocalizedString ConstructLocalizedString(string key)
        {
            return new LocalizedString(key, this);
        }

        public void SetData(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> data)
        {
            if (data == null)
            {
                Debug.LogError("Localization data is null!");
                return;
            }
            if (data.Count == 0)
            {
                Debug.LogError("Localization data is empty!");
                return;
            }
            if (_localizationData != null)
            {
                Debug.LogError("Localization data already set!");
                return;
            }
            _localizationData = data;
            UpdateInitialized();
        }

        private void UpdateInitialized()
        {
            if (_localizationData != null && 
                _localizationData.Count > 0 &&
                _currentLocale != null &&
                _defaultLocale != null &&
                _spicialFontsSet)
            {
                _isInitialized.Value = true;
            }
        }

        public void SetDefaultLocale(string localeKey)
        {
            if (string.IsNullOrEmpty(localeKey))
            {
                Debug.LogError("Locale key is null or empty!");
                return;
            }
            _defaultLocale = localeKey;
            UpdateInitialized();
        }

        public bool HasLocalizedString(string key)
        {
            if (_localizationData == null)
            {
                Debug.LogError("Localization data not set!");
                return false;
            }

            return _localizationData.TryGetValue(_currentLocale.Value, out var localeData) && localeData.TryGetValue(key, out _);
        }

        

        public bool HasSpecialFont(string localeKey, TMP_FontAsset font)
        {
            if (font == null)
            {
                Debug.LogError("Font is null!");
                return false;
            }
            return _fontsMap.TryGetValue(localeKey, out var dict) && dict.ContainsKey(font);
        }

        public bool HasSpecialFontMaterial(string localeKey, Material fontMaterial)
        {
            if (fontMaterial == null)
            {
                Debug.LogError("Font material is null!");
                return false;
            }
            return _fontMaterialsMap.TryGetValue(localeKey, out var dict) && dict.ContainsKey(fontMaterial);
        }

        public ReadOnlyDictionary<TMP_FontAsset, string> GetFontsMap(string localeKey)
        {
            return _fontsMap.TryGetValue(localeKey, out var dict) ? dict : null;
        }

        public ReadOnlyDictionary<Material, string> GetFontMaterialsMap(string localeKey)
        {
            return _fontMaterialsMap.TryGetValue(localeKey, out var dict) ? dict : null;
        }
    }
}