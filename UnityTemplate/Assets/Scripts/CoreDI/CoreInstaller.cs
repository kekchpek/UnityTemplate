using System;
using AssetsSystem;
using GMConsole;
using kekchpek.Achievements;
using kekchpek.Auxiliary.Application;
using kekchpek.Auxiliary.Time;
using kekchpek.GameSaves;
using kekchpek.Localization;
using Startup.Core;
using Zenject;

namespace DI.Core
{
    public class CoreInstaller : MonoInstaller
    {
        
        public override void InstallBindings()
        {
            Container.Bind<IProjectStartupService>().To<ProjectStartupService>().AsSingle();
            Container.Install<GameSavesInstaller>();
            Container.Bind(new Type[] {
                typeof(IAchievementsService),
                typeof(ICoreAchievementsInitializer), 
                typeof(IAchievementsAggregator), 
                typeof(IDisposable)}).To<AchievementsAggregatorServcie>().AsSingle();
            Container.Bind(new Type[] { typeof(IAchievementsModel), typeof(IAchievementsMutableModel)}).To<AchievementsModel>().AsSingle();
            Container.Bind<IAssetsModel>().To<AddressablesAssetsModel>().AsSingle();
            Container.Bind<IApplicationService>().To<ApplicationService>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind<ITimeManager>().To<TimeManager>().FromNewComponentOnNewGameObject().AsSingle();
            Container.Bind(new Type[] { typeof(IDisposable), typeof(IGameMasterCommandRegistry), typeof(IGameMasterServer) }).To<GameMasterServer>().AsSingle();
            Container.Bind<ILocalizationMutableModel>().To<LocalizationModel>().AsSingle();
            Container.Bind<ILocalizationModel>().To<ILocalizationMutableModel>().FromResolve();
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
        }
        
    }
}