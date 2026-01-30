using Cysharp.Threading.Tasks;
using GMConsole;
using kekchpek.Achievements;
using kekchpek.GameSaves;
using kekchpek.Localization;
using kekchpek.SteamApi.Achievements;
using kekchpek.SteamApi.Core;

namespace Startup.Core
{
    public class ProjectStartupService : IProjectStartupService
    {
        private readonly IGameMasterServer _gameMasterServer;
        private readonly IGameSaveManager _gameSaveManager;
        private readonly ILocalizationService _localizationService;
        private readonly ISteamInitService _steamInitService;
        private readonly ICoreAchievementsInitializer _coreAchievevementsInitializer;
        private readonly ISteamAchivementsInitializer _steamAchievementsInitializer;

        public bool IsCompleted { get; private set; } = false;

        public ProjectStartupService(
            ISteamInitService steamInitService,
            IGameMasterServer gameMasterServer,
            IGameSaveManager gameSaveManager,
            ILocalizationService localizationService,
            ICoreAchievementsInitializer coreAchievementsInitializer,
            ISteamAchivementsInitializer steamAchivementsInitializer)
        {
            _steamInitService = steamInitService;
            _gameMasterServer = gameMasterServer;
            _gameSaveManager = gameSaveManager;
            _localizationService = localizationService;
            _coreAchievevementsInitializer = coreAchievementsInitializer;
            _steamAchievementsInitializer = steamAchivementsInitializer;
        }

        public async UniTask Startup()
        {
            _gameMasterServer.StartServer();
            await _gameSaveManager.Initialize();
            await _coreAchievevementsInitializer.Initialize();
            _steamInitService.Initialize();
            _steamAchievementsInitializer.Initialize();
            await _localizationService.LoadData();
            _localizationService.SetLocale("EN");
            IsCompleted = true;
        }
    }
}
