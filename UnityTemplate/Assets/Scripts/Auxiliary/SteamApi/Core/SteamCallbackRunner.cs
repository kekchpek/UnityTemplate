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
