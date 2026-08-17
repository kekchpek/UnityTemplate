#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using kekchpek.Achievements;
using kekchpek.SteamApi.Core;

namespace kekchpek.SteamApi.Achievements
{
#if !DISABLESTEAMWORKS
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using Steamworks;
    using UnityEngine;

    public class SteamAchievementsService : IAchievementsService, ISteamAchivementsInitializer, IDisposable
    {
        private readonly ISteamInitService _steamInitService;
        private readonly IAchievementsAggregator _achivementsAggregator;
        private readonly CallResult<UserStatsReceived_t> _userStatsReceivedCallResult;
        private bool _statsReceived;

        private bool IsReady => _steamInitService.IsInitialized.Value && _statsReceived;

        public SteamAchievementsService(
            ISteamInitService steamInitService,
            IAchievementsAggregator achivementsAggregator)
        {
            _steamInitService = steamInitService;
            _achivementsAggregator = achivementsAggregator;
            _userStatsReceivedCallResult = CallResult<UserStatsReceived_t>.Create(OnUserStatsReceived);
        }

        public void Initialize()
        {
            _steamInitService.IsInitialized.Bind(OnSteamInitialized);
        }

        private void OnSteamInitialized(bool isInitialized)
        {
            if (isInitialized)
            {
                _steamInitService.IsInitialized.Unbind(OnSteamInitialized);
                RequestStats();
            }
        }

        private async void RequestStats()
        {
            SteamAPICall_t handle = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            if (handle != SteamAPICall_t.Invalid)
            {
                _userStatsReceivedCallResult.Set(handle);
                Debug.Log("[Steam Achievements] Stats request sent, waiting for response...");
            }
            else
            {
                Debug.LogWarning("[Steam Achievements] Failed to request stats.");
                await UniTask.Delay(10000);
                RequestStats();
            }
        }

        private async void OnUserStatsReceived(UserStatsReceived_t result, bool ioFailure)
        {
            if (ioFailure)
            {
                Debug.LogWarning("[Steam Achievements] IO failure while receiving stats.");
                await UniTask.Delay(10000);
                RequestStats();
                return;
            }

            if (result.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"[Steam Achievements] Failed to receive stats: {result.m_eResult}");
                await UniTask.Delay(10000);
                RequestStats();
                return;
            }
            var statsReceivedFirstTime = !_statsReceived;
            _statsReceived = true;
            Debug.Log("[Steam Achievements] Stats received successfully.");

            foreach (var achievement in GetAllAchievements())
            {
                Debug.Log($"[Steam Achievements] Achievement: {achievement.id} - {achievement.name} - {achievement.description} - {achievement.isHidden}");
            }
            if (statsReceivedFirstTime)
            {
                _achivementsAggregator.AddAchivementsService(this);
            }
        }

        private IEnumerable<(string id, string name, string description, bool isHidden)> GetAllAchievements()
        {
            if (!IsReady)
            {
                yield break;
            }

            uint numAchievements = SteamUserStats.GetNumAchievements();
            for (uint i = 0; i < numAchievements; i++)
            {
                string achievementId = SteamUserStats.GetAchievementName(i);
                string displayName = SteamUserStats.GetAchievementDisplayAttribute(achievementId, "name");
                string description = SteamUserStats.GetAchievementDisplayAttribute(achievementId, "desc");
                bool isHidden = SteamUserStats.GetAchievementDisplayAttribute(achievementId, "hidden") == "1";

                yield return (achievementId, displayName, description, isHidden);
            }
        }

        public void UnlockAchievement(string achievementId)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[Steam Achievements] Cannot unlock achievement '{achievementId}': Steam not ready.");
                return;
            }

            if (IsAchievementUnlocked(achievementId))
            {
                Debug.Log($"[Steam Achievements] Achievement '{achievementId}' is already unlocked.");
                return;
            }

            bool success = SteamUserStats.SetAchievement(achievementId);
            if (success)
            {
                SteamUserStats.StoreStats();
                Debug.Log($"[Steam Achievements] Achievement '{achievementId}' unlocked successfully.");
            }
            else
            {
                Debug.LogError($"[Steam Achievements] Failed to unlock achievement '{achievementId}'.");
            }
        }

        public void ClearAchievement(string achievementId)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"[Steam Achievements] Cannot clear achievement '{achievementId}': Steam not ready.");
                return;
            }

            if (!IsAchievementUnlocked(achievementId))
            {
                Debug.LogWarning($"[Steam Achievements] Achievement '{achievementId}' is not unlocked.");
                return;
            }

            bool success = SteamUserStats.ClearAchievement(achievementId);
            if (success)
            {
                SteamUserStats.StoreStats();
                Debug.Log($"[Steam Achievements] Achievement '{achievementId}' cleared successfully.");
            }
            else
            {
                Debug.LogWarning($"[Steam Achievements] Failed to clear achievement '{achievementId}'.");
            }
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            if (!IsReady)
            {
                return false;
            }

            if (!SteamUserStats.GetAchievement(achievementId, out bool isUnlocked))
            {
                Debug.LogWarning($"[Steam Achievements] Failed to check achievement '{achievementId}'.");
                return false;
            }

            return isUnlocked;
        }

        public void Dispose()
        {
            _achivementsAggregator.RemoveAchivementsService(this);
            _userStatsReceivedCallResult.Dispose();
        }
    }
#else
    public class SteamAchievementsService : IAchievementsService, ISteamAchivementsInitializer, IDisposable
    {
        public SteamAchievementsService(
            ISteamInitService steamInitService,
            IAchievementsAggregator achivementsAggregator)
        {
        }

        public void Initialize()
        {
        }

        public void UnlockAchievement(string achievementId)
        {
        }

        public void ClearAchievement(string achievementId)
        {
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return false;
        }

        public void Dispose()
        {
        }
    }
#endif
}
