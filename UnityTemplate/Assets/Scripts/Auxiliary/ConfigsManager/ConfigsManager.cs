using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace kekchpek.Auxiliary.Configs
{
    public class ConfigsManager : IConfigsProvider, IConfigsLoader
    {
        private const string ConfigsPathArgument = "-configsPath";
        private const string DefaultConfigsSubfolder = "Configs";

        private readonly Dictionary<string, string> _configs = new Dictionary<string, string>();

        public void LoadConfigs(string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] jsonFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string jsonText = File.ReadAllText(filePath);
                _configs[fileName] = jsonText;
            }
        }

        public void LoadDefaultConfigs()
        {
            string path = GetDefaultConfigsPath();
            LoadConfigs(path);
        }

        public T GetConfig<T>(string configName)
        {
            if (!_configs.TryGetValue(configName, out string jsonText))
                throw new KeyNotFoundException($"Config '{configName}' was not found.");

            return JsonConvert.DeserializeObject<T>(jsonText);
        }

        private static string GetDefaultConfigsPath()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == ConfigsPathArgument)
                    return args[i + 1];
            }

            return Path.Combine(UnityEngine.Application.streamingAssetsPath, DefaultConfigsSubfolder);
        }
    }
}
