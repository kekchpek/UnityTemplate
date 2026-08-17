using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace kekchpek.Auxiliary.Configs
{
    public abstract class ConfigsManagerBase : IConfigsProvider, IConfigsLoader
    {
        protected readonly Dictionary<string, string> Configs = new Dictionary<string, string>();

        public T GetConfig<T>()
        {
            var configName = typeof(T).Name;
            if (!Configs.TryGetValue(configName, out string jsonText))
                throw new KeyNotFoundException($"Config '{configName}' was not found.");

            return JsonConvert.DeserializeObject<T>(jsonText);
        }

        public UniTask<T> GetConfigAsync<T>()
        {
            var configName = typeof(T).Name;
            if (!Configs.TryGetValue(configName, out string jsonText))
                throw new KeyNotFoundException($"Config '{configName}' was not found.");

            return UniTask.FromResult(JsonConvert.DeserializeObject<T>(jsonText));
        }

        public abstract void LoadConfigs(string path);

        public abstract UniTask LoadDefaultConfigsAsync();
    }
}
