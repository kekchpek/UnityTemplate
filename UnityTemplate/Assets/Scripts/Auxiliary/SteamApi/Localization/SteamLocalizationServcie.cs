#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using kekchpek.Localization;
using kekchpek.SteamApi.Core;

namespace kekchpek.Auxiliary.SteamApi.Localization
{
#if !DISABLESTEAMWORKS
    using Steamworks;
    using UnityEngine;

    public class SteamLocalizationService : ISteamLocalizationService
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISteamInitService _steamInitService;

        public SteamLocalizationService(
            ISteamInitService steamInitService,
            ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _steamInitService = steamInitService;
        }

        public void ApplySteamLocalization()
        {
            if (!_steamInitService.IsInitialized.Value)
            {
                Debug.LogWarning("Steam is not initialized to apply localization");
                return;
            }
            var steamLanguage = SteamApps.GetCurrentGameLanguage();
            if (string.IsNullOrEmpty(steamLanguage))
            {
                Debug.LogError("Steam language is not set");
                return;
            }
            var previousLanguage = PlayerPrefs.GetString("SteamLanguage");
            if (previousLanguage == steamLanguage)
            {
                return;
            }
            PlayerPrefs.SetString("SteamLanguage", steamLanguage);
            _localizationService.SetLocale(GetLocaleKey(steamLanguage));
        }

        private static string GetLocaleKey(string steamLanguage)
        {
            return steamLanguage switch
            {
                "english" => "EN",
                "brazilian" => "PT-BR",
                "french" => "FR",
                "german" => "DE",
                "italian" => "IT",
                "japanese" => "JA",
                "koreana" => "KO",
                "polish" => "PL",
                "russian" => "RU",
                "schinese" => "ZH-CN",
                "spanish" => "ES",
                "turkish" => "TR",
                _ => "EN",
            };
        }
    }
#else
    public class SteamLocalizationService : ISteamLocalizationService
    {
        public SteamLocalizationService(
            ISteamInitService steamInitService,
            ILocalizationService localizationService)
        {
        }

        public void ApplySteamLocalization()
        {
        }
    }
#endif
}
