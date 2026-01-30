using Cysharp.Threading.Tasks;

namespace kekchpek.Localization
{
    public interface ILocalizationService
    {
        UniTask LoadData();
        void SetLocale(string localeKey);
    }
}