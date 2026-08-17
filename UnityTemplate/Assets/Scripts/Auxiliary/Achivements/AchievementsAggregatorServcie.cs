using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Diagnostics.Time;
using GMConsole;
using kekchpek.Achievements.Data;
using kekchpek.Auxiliary.Configs;
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
        private const string ClearAchievementCommand = "ClearAch";

        private readonly HashSet<IAchievementsService> _achivementsServices = new();

        private bool _isInitialized = false;

        private readonly IAchievementsMutableModel _achivementsModel;
        private readonly IConfigsProvider _configsProvider;
        private readonly IGameMasterCommandRegistry _gameMasterCommandsRegestry;

        public AchievementsAggregatorServcie(
            IAchievementsMutableModel achivementsModel,
            IConfigsProvider configsProvider,
            IGameMasterCommandRegistry gameMasterCommandRegistry)
        {
            _achivementsModel = achivementsModel;
            _configsProvider = configsProvider;
            _gameMasterCommandsRegestry = gameMasterCommandRegistry;
            _gameMasterCommandsRegestry.RegisterCommand(
                AddAchievementCommand, "Adds achievement", 
                HandleAddAchievement
            );
            _gameMasterCommandsRegestry.RegisterCommand(
                RemoveAchivementCommand, "Removes achievement", 
                HandleRemoveAchievement
            );
            _gameMasterCommandsRegestry.RegisterCommand(
                ShowAchievementsCommand, "Shows achievements", 
                HandleShowAchievements
            );
            _gameMasterCommandsRegestry.RegisterCommand(
                ClearAchievementCommand, "Clears achievement", 
                HandleClearAchievement
            );
        }

        private void HandleClearAchievement(GMArgs args) {
            foreach (var achievementId in _achivementsModel.AchievementIds) {
                ClearAchievement(achievementId);
            }
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
            var maxAchLength = 0;
            foreach (var achievementId in _achivementsModel.AchievementIds) {
                if (achievementId.Length > maxAchLength) {
                    maxAchLength = achievementId.Length;
                }
            }
            foreach (var achievementId in _achivementsModel.AchievementIds) {
                var unlockedStatus = _achivementsModel.GetAchievementUnlocked(achievementId).Value ? "Unlocked" : "Locked";
                stringBuilder.AppendLine($"{achievementId.PadRight(maxAchLength)}: {unlockedStatus}");
            }
            args.SetResult(stringBuilder.ToString());
        }

        public async UniTask Initialize()
        {
            using (TimeDebug.StartMeasure("AchievementsAggregatorServcie.Initialize"))
            {
                var config = await _configsProvider.GetConfigAsync<AchievementsConfig>();
                _achivementsModel.SetupAchievements(config.AchievementIds);
                _isInitialized = true;
            }
        }

        public void AddAchivementsService(IAchievementsService achivementsService)
        {
            if (!_isInitialized) {
                Debug.LogError("AchievementsAggregatorServcie is not initialized");
                return;
            }
            if (_achivementsServices.Contains(achivementsService)) {
                Debug.LogError("AchievementsAggregatorServcie already contains this achivements service");
                return;
            }
            _achivementsServices.Add(achivementsService);
            foreach (var achievementId in _achivementsModel.AchievementIds) {
                if (_achivementsModel.GetAchievementUnlocked(achievementId).Value && 
                    !achivementsService.IsAchievementUnlocked(achievementId)) 
                {
                    achivementsService.UnlockAchievement(achievementId);
                }
            }
        }

        public void RemoveAchivementsService(IAchievementsService achivementsService)
        {
            if (!_isInitialized) {
                Debug.LogError("AchievementsAggregatorServcie is not initialized");
                return;
            }
            _achivementsServices.Remove(achivementsService);
        }

        public void UnlockAchievement(string achievementId)
        {
            if (!_isInitialized) {
                Debug.LogError("AchievementsAggregatorServcie is not initialized");
                return;
            }
            foreach (var achivementsService in _achivementsServices) 
            {
                achivementsService.UnlockAchievement(achievementId);
            }
            _achivementsModel.UnlockAchievement(achievementId);
        }

        public void ClearAchievement(string achievementId)
        {
            if (!_isInitialized) {
                Debug.LogError("AchievementsAggregatorServcie is not initialized");
                return;
            }
            foreach (var achivementsService in _achivementsServices) 
            {
                achivementsService.ClearAchievement(achievementId);
            }
            _achivementsModel.ClearAchievement(achievementId);
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            if (!_isInitialized) {
                Debug.LogError("AchievementsAggregatorServcie is not initialized");
                return false;
            }
            return _achivementsModel.GetAchievementUnlocked(achievementId).Value;
        }

        public void Dispose() {
            _gameMasterCommandsRegestry.UnregisterCommand(AddAchievementCommand);
            _gameMasterCommandsRegestry.UnregisterCommand(RemoveAchivementCommand);
            _gameMasterCommandsRegestry.UnregisterCommand(ShowAchievementsCommand);
            _gameMasterCommandsRegestry.UnregisterCommand(ClearAchievementCommand);
        }
    }
}