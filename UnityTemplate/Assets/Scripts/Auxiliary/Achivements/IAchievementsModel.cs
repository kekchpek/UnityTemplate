using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;

namespace kekchpek.Achievements
{
    public interface IAchievementsModel
    {
        ReadOnlySpan<string> AchievementIds { get; }
        IBindable<bool> GetAchievementUnlocked(string achievementId);
    }
}