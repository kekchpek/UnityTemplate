using System.Collections.Generic;
using Newtonsoft.Json;

namespace kekchpek.Achievements.Data
{
    public class AchievementsConfig
    {
        [JsonProperty]
        private List<string> achievementIds;

        public IReadOnlyList<string> AchievementIds => achievementIds;
    }
}