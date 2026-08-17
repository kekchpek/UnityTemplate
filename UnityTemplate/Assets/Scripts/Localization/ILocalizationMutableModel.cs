using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization
{
    public interface ILocalizationMutableModel : ILocalizationModel
    {
        void SetLocale(string localeKey);
        void SetData(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> data);
        void SetDefaultLocale(string localeKey);
        void SetSpecialFonts(
            Dictionary<TMP_FontAsset, TMP_FontAsset> fonts,
            Dictionary<Material, Material> fontMaterials);
        void SetupFontData(FontsConfig fontsConfig);
        void ClearSpecialFonts();
    }
}