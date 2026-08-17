using System;
using Newtonsoft.Json;

namespace SkillsSystem.Data
{
    public class SkillData
    {
        [JsonProperty]
        private readonly string id;

        [JsonProperty]
        private readonly SkillResourceData[][] upgradeCost;

        [JsonProperty]
        private readonly string nameKey;

        [JsonProperty]
        private readonly string descriptionKey;

        public string Id => id;
        public int MaxUpgrades => upgradeCost.Length;
        public ReadOnlySpan<SkillResourceData> GetUpgradeCost(int index) => upgradeCost[index];
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
    }
}
