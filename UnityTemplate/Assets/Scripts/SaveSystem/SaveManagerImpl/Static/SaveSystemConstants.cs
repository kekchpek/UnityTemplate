using System.Collections.Generic;

namespace kekchpek.GameSaves.Static
{
    public static class SaveSystemContacts
    {
        public const string SaveFolder = "saves";
        public const string DataFolder = "data";
        public const string CommonSaveFile = "common";
        public const string SettingsSaveFile = "settings";

        public const long SettingsDebounceIntervalMs = 10000L;
        public const long CommonDataDebounceIntervalMs = 10000L;

        public const int MaxSaveOnChangesTimeMs = 100000000;

        public static readonly IReadOnlyList<(int size, int elementSize, int count)> PrewarmedBuffers =
            new (int size, int elementSize, int count)[0];
    }
}
