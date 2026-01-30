using System;
using System.Collections.Generic;
using UnityEngine;

namespace kekchpek.Localization
{
    public class LocalizationModel : ILocalizationMutableModel
    {

        private const string MissingString = "<MISSING_STRING>";
        
        public event Action OnLocaleChanged;

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _localizationData;
        private string _currentLocale;
        private string _defaultLocale;

        public string GetCurrentLocale()
        {
            return _currentLocale;
        }

        public string GetLocalizedString(string key)
        {
            if (_localizationData == null)
            {
                Debug.LogError("Localization data not set!");
                return MissingString;
            }

            string stringToReturn = null;
            if (_localizationData.TryGetValue(_currentLocale, out var localeData))
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
            _currentLocale = localeKey;
            OnLocaleChanged?.Invoke();
        }

        public void SetData(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> data)
        {
            _localizationData = data;
        }

        public void SetDefaultLocale(string localeKey)
        {
            _defaultLocale = localeKey;
        }
    }
}