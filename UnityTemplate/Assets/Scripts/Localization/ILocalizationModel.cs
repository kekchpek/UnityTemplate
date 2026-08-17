using System;
using System.Collections.ObjectModel;
using AsyncReactAwait.Bindable;
using TMPro;
using UnityEngine;

namespace kekchpek.Localization
{
    public interface ILocalizationModel
    {
        event Action NewLocaleReadyToBeDisplayed;
        IBindable<bool> IsInitialized { get; }
        IBindable<string> CurrentLocale { get; }
        IBindable<bool> IsSpecialFontsLoaded { get; }
        LocalizedString ConstructLocalizedString(Func<ILocalizationModel, string> stringBuilder);
        LocalizedString ConstructLocalizedString(string key);
        bool HasSpecialFont(string localeKey, TMP_FontAsset font);
        bool HasSpecialFontMaterial(string localeKey, Material fontMaterial);
        string GetLocalizedString(string key);
        bool HasLocalizedString(string key);
        TMP_FontAsset GetSpecialFont(TMP_FontAsset font);
        Material GetSpecialFontMaterial(Material font);
        ReadOnlyDictionary<TMP_FontAsset, string> GetFontsMap(string localeKey);
        ReadOnlyDictionary<Material, string> GetFontMaterialsMap(string localeKey);
    }
}