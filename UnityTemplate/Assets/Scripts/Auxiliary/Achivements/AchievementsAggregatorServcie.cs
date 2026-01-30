using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssetsSystem;
using Cysharp.Threading.Tasks;
using GMConsole;
using kekchpek.Achievements.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace kekchpek.Achievements
{
    public class AchievementsAggregatorServcie : 
        IAchievementsAggregator
        , ICoreAchievementsInitializer
        , IAchievementsService
        , IDisposable
    {

        private const string AddAchievementCommand = "AddAch";
        private const string RemoveAchivementCommand = "RemoveAch";
        private const string ShowAchievementsCommand = "ShowAch";

        private const string AchievementsConfigPath = "Configs/AchievementsConfig";

        private readonly HashSet<IAchievementsService> _achivementsServices = new();

        private readonly IAchievementsMutableModel _achivementsModel;
        private readonly IAssetsModel _assetsModel;
        private readonly IGameMasterCommandRegistry _gameMasterCommandsRegestry;

        public AchievementsAggregatorServcie(
            IAchievementsMutableModel achivementsModel,
            IAssetsModel assetsModel,
            IGameMasterCommandRegistry gameMasterCommandRegistry)
        {
            _achivementsModel = achivementsModel;
            _assetsModel = assetsModel;
            _gameMasterCommandsRegestry = gameMasterCommandRegistry;
            _gameMasterCommandsRegestry.RegisterCommand(
                AddAchievementCommand, "Adds achievement", 
                HandleAddAchievement
            );
            _gameMasterCommandsRegestry.RegisterCommand(
                AddAchievementCommand, "Removes achievement", 
                HandleRemoveAchievement
            );
            _gameMasterCommandsRegestry.RegisterCommand(
                ShowAchievementsCommand, "Shows achievements", 
                HandleShowAchievements
            );
        }

        private void HandleAddAchievement(GMArgs args) {
            var achId = args.GetString();
            UnlockAchievement(achId);
        }

        private void HandleRemoveAchievement(GMArgs args) {
            var achId = args.GetString();
            ClearAchievement(achId);
        }

        private void HandleShowAchievements(GMArgs args) {
            var stringBuilder = new StringBuilder();
            var maxAchLength = _achivementsModel.AchievementIds.Select(x => x.Length).Max();
            foreach (var achievementId in _achivementsModel.AchievementIds) {
                var unlockedStatus = _achivementsModel.GetAchievementUnlocked(achievementId).Value ? "Unlocked" : "Locked";
                stringBuilder.AppendLine($"{achievementId.PadRight(maxAchLength)}: {unlockedStatus}");
            }
            args.SetResult(stringBuilder.ToString());
        }

        public async UniTask Initialize()
        {
            var achievementsConfigText = await _assetsModel.LoadAsset<TextAsset>(AchievementsConfigPath);
            var config = JsonConvert.DeserializeObject<AchievementsConfig>(achievementsConfigText.text);
            _achivementsModel.SetupAchievements(config.AchievementIds);
            _assetsModel.ReleaseLoadedAssets(AchievementsConfigPath);
        }

        public void AddAchivementsService(IAchievementsService achivementsService)
        {
            _achivementsServices.Add(achivementsService);
            
        }

        public void RemoveAchivementsService(IAchievementsService achivementsService)
        {
            _achivementsServices.Remove(achivementsService);
        }

        public void UnlockAchievement(string achievementId)
        {
            foreach (var achivementsService in _achivementsServices) 
            {
                achivementsService.UnlockAchievement(achievementId);
            }
            _achivementsModel.UnlockAchievement(achievementId);
        }

        public void ClearAchievement(string achievementId)
        {
            foreach (var achivementsService in _achivementsServices) 
            {
                achivementsService.ClearAchievement(achievementId);
            }
            _achivementsModel.ClearAchievement(achievementId);
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return _achivementsModel.GetAchievementUnlocked(achievementId).Value;
        }

        public void Dispose() {
            _gameMasterCommandsRegestry.UnregisterCommand(AddAchievementCommand);
            _gameMasterCommandsRegestry.UnregisterCommand(RemoveAchivementCommand);
        }
    }
}