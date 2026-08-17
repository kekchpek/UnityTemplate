using Cysharp.Threading.Tasks;
using Diagnostics.Time;
using GMConsole;
using kekchpek.Achievements;
using kekchpek.Auxiliary.Configs;
using kekchpek.Auxiliary.SteamApi.Localization;
using kekchpek.GameSaves;
using kekchpek.Localization;
using kekchpek.SteamApi.Achievements;
using kekchpek.SteamApi.Core;
using Zenject;

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
        private readonly IConfigsLoader _configsLoader;
        private readonly IGameProjectStartupService _gameProjectStartupService;
        private readonly ISteamLocalizationService _steamLocalizationService;
        
        public bool IsCompleted { get; private set; } = false;

        private UniTaskCompletionSource _startupCompletionSource;

        public ProjectStartupService(
            ISteamInitService steamInitService,
            IGameMasterServer gameMasterServer,
            IGameSaveManager gameSaveManager,
            ILocalizationService localizationService,
            ICoreAchievementsInitializer coreAchievementsInitializer,
            ISteamAchivementsInitializer steamAchivementsInitializer,
            ISteamLocalizationService steamLocalizationService,
            IConfigsLoader configsLoader,
            [InjectOptional] IGameProjectStartupService gameProjectStartupService)
        {
            _steamInitService = steamInitService;
            _gameMasterServer = gameMasterServer;
            _gameSaveManager = gameSaveManager;
            _localizationService = localizationService;
            _coreAchievevementsInitializer = coreAchievementsInitializer;
            _steamAchievementsInitializer = steamAchivementsInitializer;
            _steamLocalizationService = steamLocalizationService;
            _configsLoader = configsLoader;
            _gameProjectStartupService = gameProjectStartupService;
        }

        public async UniTask Startup()
        {
            using (TimeDebug.StartMeasure("ProjectStartupService.Startup"))
            {
                if (IsCompleted)
                {
                    return;
                }
                if (_startupCompletionSource != null)
                {
                    await _startupCompletionSource.Task;
                    return;
                }
                _startupCompletionSource = new UniTaskCompletionSource();
                await _configsLoader.LoadDefaultConfigsAsync();
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                _gameMasterServer.StartServer();
                #endif
                await _gameSaveManager.Initialize();
                await _coreAchievevementsInitializer.Initialize();
                _steamInitService.Initialize();
                _steamAchievementsInitializer.Initialize();
                _steamLocalizationService.ApplySteamLocalization();
                await _localizationService.LoadData();
                if (_gameProjectStartupService != null)
                {
                    await _gameProjectStartupService.Startup();
                }
                IsCompleted = true;
            }
        }
    }
}
