using System.Collections.Generic;

namespace kekchpek.Achievements
{
    public interface IAchievementsMutableModel : IAchievementsModel
    {
        void SetupAchievements(IReadOnlyList<string> achievementIds);
        void UnlockAchievement(string achievementId);
        void ClearAchievement(string achievementId);
    }
}