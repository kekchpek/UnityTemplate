using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace kekchpek.GameSaves.Data
{
    public sealed class GameSavesConfig
    {
        private static readonly IReadOnlyList<PrewarmBufferEntry> emptyPrewarmedBuffers = Array.Empty<PrewarmBufferEntry>();

        [JsonProperty("saveDataFolder")]
        private string saveDataFolder;

        [JsonProperty("saveFolder")]
        private string saveFolder;

        [JsonProperty("dataFolder")]
        private string dataFolder;

        [JsonProperty("customCodecsVersion")]
        private int customCodecsVersion;

        [JsonProperty("commonSaveFile")]
        private string commonSaveFile;

        [JsonProperty("settingsSaveFile")]
        private string settingsSaveFile;

        [JsonProperty("settingsDebounceIntervalMs")]
        private long settingsDebounceIntervalMs;

        [JsonProperty("commonDataDebounceIntervalMs")]
        private long commonDataDebounceIntervalMs;

        [JsonProperty("maxSaveOnChangesTimeMs")]
        private int maxSaveOnChangesTimeMs;

        [JsonProperty("autosaveEnabled")]
        private bool autosaveEnabled;

        [JsonProperty("autosaveIntervalMs")]
        private long autosaveIntervalMs;

        [JsonProperty("prewarmedBuffers")]
        private List<PrewarmBufferEntry> prewarmedBuffers;

        public string SaveDataFolder => saveDataFolder;

        public string SaveFolder => saveFolder;

        public string DataFolder => dataFolder;

        public int CustomCodecsVersion => customCodecsVersion;

        public string CommonSaveFile => commonSaveFile;

        public string SettingsSaveFile => settingsSaveFile;

        public long SettingsDebounceIntervalMs => settingsDebounceIntervalMs;

        public long CommonDataDebounceIntervalMs => commonDataDebounceIntervalMs;

        public int MaxSaveOnChangesTimeMs => maxSaveOnChangesTimeMs;

        public bool AutosaveEnabled => autosaveEnabled;

        public long AutosaveIntervalMs => autosaveIntervalMs;

        public IReadOnlyList<PrewarmBufferEntry> PrewarmedBuffers => prewarmedBuffers ?? emptyPrewarmedBuffers;
    }
}
