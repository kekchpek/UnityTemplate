#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if !DISABLESTEAMWORKS

using Steamworks;
using UnityEngine;

namespace kekchpek.SteamApi.Core
{
    public class SteamCallbackRunner : MonoBehaviour
    {
        private void Update()
        {
            SteamAPI.RunCallbacks();
        }
    }
}

#endif
