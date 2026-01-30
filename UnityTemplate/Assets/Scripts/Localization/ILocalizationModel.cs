using System;

namespace kekchpek.Localization
{
    public interface ILocalizationModel
    {
        event Action OnLocaleChanged;
        string GetCurrentLocale();
        string GetLocalizedString(string key);
    }
}