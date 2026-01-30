using System.Collections.Generic;

namespace kekchpek.Localization
{
    public interface ILocalizationMutableModel : ILocalizationModel
    {
        void SetLocale(string localeKey);
        void SetData(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> data);
        void SetDefaultLocale(string localeKey);
    }
}