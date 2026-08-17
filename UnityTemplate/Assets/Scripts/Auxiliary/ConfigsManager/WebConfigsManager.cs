#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace kekchpek.Auxiliary.Configs
{
    public class WebConfigsManager : ConfigsManagerBase
    {
        private const string ConfigsPathArgument = "-configsPath";
        private const string DefaultConfigsSubfolder = "Configs";

        private static readonly string[] DefaultConfigNames =
        {
            "AchievementsConfig",
            "AutoclickerConfig",
            "ClosetConfig",
            "ClosetRewardsConfig",
            "DialogueActivationConfig",
            "DialogueConfig",
            "GameSavesConfig",
            "GodsExperienceConfig",
            "GlyphsConfig",
            "SkillTreeConfig",
            "SkillsConfig",
            "SkillsDiscoveringConfig",
            "UnityTemplateSkillsConfig",
            "SpecialAbilitiesConfig",
            "StartButtonTextsConfig"
        };

        public override void LoadConfigs(string path)
        {
            throw new NotSupportedException("LoadConfigs with a custom path is not supported on WebGL.");
        }

        public override async UniTask LoadDefaultConfigsAsync()
        {
            var configsBaseUrl = GetDefaultConfigsBaseUrl();
            Configs.Clear();

            foreach (var configName in DefaultConfigNames)
            {
                var configUrl = BuildConfigUrl(configsBaseUrl, configName);
                var jsonText = await LoadConfigText(configUrl);
                Configs[configName] = jsonText;
            }
        }

        private static string GetDefaultConfigsBaseUrl()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ConfigsPathArgument)
                    return args[i + 1].TrimEnd('/');
            }

            return $"{UnityEngine.Application.streamingAssetsPath}/{DefaultConfigsSubfolder}";
        }

        private static string BuildConfigUrl(string configsBaseUrl, string configName)
        {
            return $"{configsBaseUrl}/{configName}.json";
        }

        private static async UniTask<string> LoadConfigText(string url)
        {
            using var request = UnityWebRequest.Get(url);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Failed to load config from '{url}': {request.error}");

            return request.downloadHandler.text;
        }
    }
}

#endif
