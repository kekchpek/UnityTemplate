using System;
using AsyncReactAwait.Bindable;

namespace kekchpek.Localization
{
    public interface ILocalizationModel
    {
        IBindable<string> CurrentLocale { get; }
        string GetLocalizedString(string key);
    }
}