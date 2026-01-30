using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Achievements
{
    public interface IAchievementsModel
    {
        ICollection<string> AchievementIds { get; }
        IBindable<bool> GetAchievementUnlocked(string achievementId);
    }
}