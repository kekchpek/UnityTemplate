#if !UNITY_WEBGL || UNITY_EDITOR

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Diagnostics.Time;

namespace kekchpek.Auxiliary.Configs
{
    public class FileConfigsManager : ConfigsManagerBase
    {
        private const string ConfigsPathArgument = "-configsPath";
        private const string DefaultConfigsSubfolder = "Configs";

        public override void LoadConfigs(string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] jsonFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string jsonText = File.ReadAllText(filePath);
                Configs[fileName] = jsonText;
            }
        }

        public override UniTask LoadDefaultConfigsAsync()
        {
            using (TimeDebug.StartMeasure("FileConfigsManager.LoadDefaultConfigsAsync"))
            {
                LoadConfigs(GetDefaultConfigsPath());
            }
            return UniTask.CompletedTask;
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

#endif
