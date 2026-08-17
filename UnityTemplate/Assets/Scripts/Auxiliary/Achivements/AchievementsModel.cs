using System;
using System.Collections.Generic;
using AsyncReactAwait.Bindable;
using kekchpek.GameSaves;
using UnityEngine;

namespace kekchpek.Achievements
{
    public class AchievementsModel : IAchievementsMutableModel
    {

        private string[] _achievementIds;
        private readonly Dictionary<string, IMutable<bool>> _achievements = new();

        private readonly IGameSaveManager _gameSaveManager;

        public AchievementsModel(IGameSaveManager gameSaveManager)
        {
            _gameSaveManager = gameSaveManager;
        }

        public ReadOnlySpan<string> AchievementIds => _achievementIds;


        public void SetupAchievements(IReadOnlyList<string> achievementIds)
        {
            var achievementIdsArray = new string[achievementIds.Count];
            for (int i = 0; i < achievementIds.Count; i++) {
                achievementIdsArray[i] = achievementIds[i];
            }
            _achievementIds = achievementIdsArray;
            _gameSaveManager.IsInitialized.Bind(OnGameSaveManagerInitialized);
        }

        private void OnGameSaveManagerInitialized(bool isInitialized)
        {
            if (isInitialized)
            {
                LoadData();
            }
            _gameSaveManager.IsInitialized.Unbind(OnGameSaveManagerInitialized);
        }

        private void LoadData()
        {
            var dataProvider = _gameSaveManager.GetExclusiveDataProvider("Achievements");
            foreach (var achievementId in _achievementIds)
            {
                _achievements[achievementId] = 
                    dataProvider.DeserializeAndCaptureStructValue(achievementId, false);
            }
        }

        public void ClearAchievement(string achievementId)
        {
            if (_achievements.TryGetValue(achievementId, out var achievement))
            {
                achievement.Value = false;
            }
            else 
            {
                Debug.LogError($"Achievement '{achievementId}' not found");
            }
        }

        public IBindable<bool> GetAchievementUnlocked(string achievementId)
        {
            if (_achievements.TryGetValue(achievementId, out var achievement))
            {
                return achievement;
            }
            else 
            {
                Debug.LogError($"Achievement '{achievementId}' not found");
                return new Mutable<bool>(false);
            }
        }

        public void UnlockAchievement(string achievementId)
        {
            if (_achievements.TryGetValue(achievementId, out var achievement))
            {
                achievement.Value = true;
            }
            else 
            {
                Debug.LogError($"Achievement '{achievementId}' not found");
            }
        }
    }
}