using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillsSystem.Data
{
    public class SkillsConfig
    {
        [JsonProperty]
        private List<SkillData> skills;

        public IReadOnlyList<SkillData> Skills => skills;
    }
}
