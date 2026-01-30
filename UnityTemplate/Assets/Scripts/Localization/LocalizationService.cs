using System.Collections.Generic;
using System.IO;
using AssetsSystem;
using Cysharp.Threading.Tasks;
using Diagnostics.Time;
using kekchpek.Localization.Static;
using UnityEngine;

namespace kekchpek.Localization
{
    public class LocalizationService : ILocalizationService
    {

        private static readonly IReadOnlyList<string> Locales = new[]
        {
            "EN",
            "RU"
        };

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

                    _localizationMutableModel.SetData(localizationDataDict);
                }
            }
            
            _localizationMutableModel.SetDefaultLocale(Locales[0]);
            _localizationMutableModel.SetLocale(Locales[0]);
        }

        public void SetLocale(string localeKey)
        {
            _localizationMutableModel.SetLocale(localeKey);
        }
    }
}