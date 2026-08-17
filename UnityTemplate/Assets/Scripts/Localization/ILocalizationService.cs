using System;
using AsyncReactAwait.Bindable;
using Cysharp.Threading.Tasks;
using TMPro;

namespace kekchpek.Localization
{
    public interface ILocalizationService
    {
        UniTask LoadData();
        void SetLocale(string localeKey);
    }
}