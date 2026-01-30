using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming due to json naming convention

namespace kekchpek.GameSaves.Data
{
    [Serializable]
    public class SaveConfig
    {

        [JsonProperty]
        private List<(int size, int elementSize, int count)> prewarmedBuffers;

        [JsonProperty]
        private string saveFolder;

        [JsonProperty]
        private string dataFolder;

        [JsonProperty]
        private string commonSaveFile;

        [JsonProperty]
        private string settingsSaveFile;

        [JsonProperty]
        private long autosaveIntervalMs;

        [JsonProperty]
        private long settingsDebounceIntervalMs;

        [JsonProperty]
        private long commonDataDebounceIntervalMs;

        [JsonProperty]
        private bool autosaveEnabled;


        public IReadOnlyList<(int size, int elementSize, int count)> PrewarmedBuffers => prewarmedBuffers;
        public string SaveFolder => saveFolder;
        public string DataFolder => dataFolder;
        public string CommonSaveFile => commonSaveFile;
        public string SettingsSaveFile => settingsSaveFile;

        public bool AutosaveEnabled => autosaveEnabled;
        public long AutosaveIntervalMs => autosaveIntervalMs;
        public long SettingsDebounceIntervalMs => settingsDebounceIntervalMs;
        public long CommonDataDebounceIntervalMs => commonDataDebounceIntervalMs;
    }
}