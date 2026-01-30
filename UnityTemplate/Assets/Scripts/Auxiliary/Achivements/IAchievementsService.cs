using Cysharp.Threading.Tasks;

namespace kekchpek.Achievements
{
    public interface IAchievementsService
    {
        void UnlockAchievement(string achievementId);
        void ClearAchievement(string achievementId);
        bool IsAchievementUnlocked(string achievementId);
    }
}
