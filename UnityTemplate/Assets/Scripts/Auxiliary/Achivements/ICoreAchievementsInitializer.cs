using Cysharp.Threading.Tasks;

namespace kekchpek.Achievements
{
    public interface ICoreAchievementsInitializer
    {
        UniTask Initialize();
    }
}