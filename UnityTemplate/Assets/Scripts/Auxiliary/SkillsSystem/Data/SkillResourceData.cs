using System;
using Newtonsoft.Json;

namespace SkillsSystem.Data
{
    [Serializable]
    public class SkillResourceData
    {

        [JsonProperty]
        private readonly string resourceId;

        [JsonProperty]
        private readonly float amount;

        public string ResourceId => resourceId;
        public float Amount => amount;
    }
}