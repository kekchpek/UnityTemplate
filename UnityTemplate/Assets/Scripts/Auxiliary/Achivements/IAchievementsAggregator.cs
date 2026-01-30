namespace kekchpek.Achievements
{
    public interface IAchievementsAggregator
    {
        void AddAchivementsService(IAchievementsService achivementsService);
        void RemoveAchivementsService(IAchievementsService achivementsService);
    }
}