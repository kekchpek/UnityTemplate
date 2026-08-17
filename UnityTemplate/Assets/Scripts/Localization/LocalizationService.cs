using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AssetsSystem;
using AsyncReactAwait.Bindable;
using Cysharp.Threading.Tasks;
using Diagnostics.Time;
using kekchpek.Localization.Static;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization
{
    public class LocalizationService : ILocalizationService
    {

        private const string FontsConfigPath = "FontsConfig";

        public static readonly IReadOnlyList<string> Locales = new[]
        {
            "EN",
            "FR",
            "DE",
            "ES",
            "IT",
            "PT-BR",
            "PL",
            "RU",
            "UA",
            "LT",
            "TR",
            "JA",
            "KO",
            "ZH-CN",
        };

        private readonly Dictionary<string, Task> _loadingTasks = new();

        private readonly ILocalizationMutableModel _localizationMutableModel;
        private readonly IAssetsModel _assetsModel;

        public LocalizationService(ILocalizationMutableModel localizationMutableModel,
            IAssetsModel assetsModel)
        {
            _localizationMutableModel = localizationMutableModel;
            _assetsModel = assetsModel;
        }

        public async UniTask LoadData()
        {
            using (TimeDebug.StartMeasure("Loading localization"))
            {
                await UniTask.WhenAll(
                    LoadStrings(),
                    LoadFonts()
                );
                if (_localizationMutableModel.CurrentLocale.Value == null)
                {
                    // fonts setup on locale change
                    SetLocale(GetLocaleFromSystemSettings());
                }
                else
                {
                    SetupFontsForLocale(_localizationMutableModel.CurrentLocale.Value);
                }
            }
            
            _localizationMutableModel.SetDefaultLocale(Locales[0]);
        }

        private async UniTask LoadFonts() 
        {
            var fontsConfig = await _assetsModel.LoadAsset<FontsConfig>(FontsConfigPath);
            _localizationMutableModel.SetupFontData(fontsConfig);
        }

        private async UniTask LoadStrings() {
            var localizationDataDict = new Dictionary<string, IReadOnlyDictionary<string, string>>();
            var localizationDataMutableDict = new Dictionary<string, Dictionary<string, string>>();
            foreach (var locale in Locales)
            {
                var dict = new Dictionary<string, string>();
                localizationDataDict.Add(locale, dict);
                localizationDataMutableDict.Add(locale, dict);
                var localizationData = await _assetsModel.LoadAsset<TextAsset>(LocalizationPaths.LocalePath(locale));
                using var memoryStream = new MemoryStream(localizationData.bytes);
                using var reader = new StreamReader(memoryStream);
                while (await reader.ReadLineAsync() is { } line)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                    {
                        continue;
                    }
                    var data = line.Split("=", 2);
                    localizationDataMutableDict[locale].Add(data[0], data[1]);
                }

            }
            _localizationMutableModel.SetData(localizationDataDict);
        }

        private string GetLocaleFromSystemSettings()
        {
            var locale = Application.systemLanguage switch
            {
                SystemLanguage.English => "EN",
                SystemLanguage.French => "FR",
                SystemLanguage.German => "DE",
                SystemLanguage.Spanish => "ES",
                SystemLanguage.Italian => "IT",
                SystemLanguage.Portuguese => "PT-BR",
                SystemLanguage.Polish => "PL",
                SystemLanguage.Russian => "RU",
                SystemLanguage.Ukrainian => "UA",
                SystemLanguage.Lithuanian => "LT",
                SystemLanguage.Turkish => "TR",
                SystemLanguage.Japanese => "JA",
                SystemLanguage.Korean => "KO",
                SystemLanguage.ChineseSimplified => "ZH-CN",
                SystemLanguage.Chinese => "ZH-CN",
                _ => Locales[0],
            };

            foreach (var supportedLocale in Locales)
            {
                if (supportedLocale == locale)
                {
                    return locale;
                }
            }

            return Locales[0];
        }

        public void SetLocale(string localeKey)
        {
            var previousLocale = _localizationMutableModel.CurrentLocale.Value;
            ClearFontsForLocale(previousLocale);
            _localizationMutableModel.ClearSpecialFonts();
            _localizationMutableModel.SetLocale(localeKey);
            SetupFontsForLocale(localeKey);
        }
        private void ClearFontsForLocale(string localeKey)
        {
            if (string.IsNullOrEmpty(localeKey))
            {
                return;
            }
            if (_loadingTasks.ContainsKey(localeKey))
            {
                // loading task automatically clears fonts for locale
                // if the locale is expired
                return;
            }

            if (_localizationMutableModel.GetFontsMap(localeKey) == null) 
            {
                return;
            }
            var fontsMap = _localizationMutableModel.GetFontsMap(localeKey);
            var fontMaterialsMap = _localizationMutableModel.GetFontMaterialsMap(localeKey);
            foreach (var font in fontsMap.Values)
            {
                _assetsModel.ReleaseLoadedAssets(font);
            }
            foreach (var fontMaterial in fontMaterialsMap.Values)
            {
                _assetsModel.ReleaseLoadedAssets(fontMaterial);
            }
        }

        private void SetupFontsForLocale(string localeKey)
        {
            if (_localizationMutableModel.GetFontsMap(localeKey) != null)
            {
                if (!_loadingTasks.TryGetValue(localeKey, out var task))
                {
                    var newTask = LoadFonts(localeKey);
                    _loadingTasks.Add(localeKey, newTask);
                    newTask.ContinueWith(t => {
                        _loadingTasks.Remove(localeKey);
                        if (t.Exception != null)
                        {
                            Debug.LogException(t.Exception);
                        }
                    });
                }
            }
        }

        private async Task LoadFonts(string localeKey)
        {
            if (_localizationMutableModel.GetFontsMap(localeKey) == null) 
            {
                return;
            }
            var fontsMap = _localizationMutableModel.GetFontsMap(localeKey);
            var fontMaterialsMap = _localizationMutableModel.GetFontMaterialsMap(localeKey);
            Task[] tasks = new Task[fontsMap.Count + fontMaterialsMap.Count];
            int i = 0;
            foreach (var fontPair in fontsMap)
            {
                tasks[i] = _assetsModel.LoadAsset<TMP_FontAsset>(fontPair.Value);
                i++;
            }
            foreach (var fontMaterialPair in fontMaterialsMap)
            {
                tasks[i] = _assetsModel.LoadAsset<Material>(fontMaterialPair.Value);
                i++;
            }
            await Task.WhenAll(tasks);
            Dictionary<TMP_FontAsset, TMP_FontAsset> fonts = new();
            Dictionary<Material, Material> fontMaterials = new();
            foreach (var fontPair in fontsMap)
            {
                if (_assetsModel.TryGetCachedAsset<TMP_FontAsset>(fontPair.Value, out var cachedFont))
                {
                    fonts.Add(fontPair.Key, cachedFont);
                }
                else
                {
                    Debug.LogError($"Font {fontPair.Value} not found in cache!");
                    fonts.Add(fontPair.Key, fontPair.Key);
                }
            }
            foreach (var fontMaterialPair in fontMaterialsMap)
            {
                if (_assetsModel.TryGetCachedAsset<Material>(fontMaterialPair.Value, out var cachedFontMaterial))
                {
                    fontMaterials.Add(fontMaterialPair.Key, cachedFontMaterial);
                }
                else
                {
                    Debug.LogError($"Font material {fontMaterialPair.Value} not found in cache!");
                    fontMaterials.Add(fontMaterialPair.Key, fontMaterialPair.Key);
                }
            }
            if (_localizationMutableModel.CurrentLocale.Value == localeKey)
            {
                _localizationMutableModel.SetSpecialFonts(fonts, fontMaterials);
            }
            else
            {
                foreach (var font in fontsMap.Values)
                {
                    _assetsModel.ReleaseLoadedAssets(font);
                }
                foreach (var fontMaterialPair in fontMaterialsMap)
                {
                    _assetsModel.ReleaseLoadedAssets(fontMaterialPair.Value);
                }
            }
        }
    }
}