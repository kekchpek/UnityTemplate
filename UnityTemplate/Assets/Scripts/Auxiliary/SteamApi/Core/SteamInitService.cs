using System;
using AsyncReactAwait.Bindable;
using Steamworks;
using UnityEngine;

namespace kekchpek.SteamApi.Core
{
    public class SteamInitService : ISteamInitService, IDisposable
    {
        private readonly Mutable<bool> _isInitialized = new(false);
        private GameObject _callbackRunnerObject;

        public IBindable<bool> IsInitialized => _isInitialized;

        public void Initialize()
        {
            if (_isInitialized.Value)
            {
                return;
            }

            try
            {
                if (!Packsize.Test())
                {
                    Debug.LogError("[Steamworks.NET] Packsize Test returned false. " +
                                   "The wrong version of Steamworks.NET is being run in this platform.");
                    return;
                }

                if (!DllCheck.Test())
                {
                    Debug.LogError("[Steamworks.NET] DllCheck Test returned false. " +
                                   "One or more of the Steamworks binaries seems to be the wrong version.");
                    return;
                }

                if (!SteamAPI.Init())
                {
                    Debug.LogError("[Steamworks.NET] SteamAPI.Init() failed. " +
                                   "Ensure Steam client is running and steam_appid.txt is present.");
                    return;
                }
                CreateCallbackRunner();
                _isInitialized.Value = true;
                Debug.Log("[Steamworks.NET] Steam API initialized successfully.");

            }
            catch (DllNotFoundException e)
            {
                Debug.LogError($"[Steamworks.NET] Could not load steam_api dll: {e.Message}");
            }

            if (SteamUtils.IsOverlayEnabled())
            {
                Debug.Log("[Steamworks.NET] Steam Overlay is enabled");
            }
            else 
            {
                Debug.Log("[Steamworks.NET] Steam Overlay is disabled");
            }
        }

        private void CreateCallbackRunner()
        {
            _callbackRunnerObject = new GameObject("[Steam Callback Runner]");
            _callbackRunnerObject.AddComponent<SteamCallbackRunner>();
            UnityEngine.Object.DontDestroyOnLoad(_callbackRunnerObject);
        }

        public void Dispose()
        {
            if (_isInitialized.Value)
            {
                if (_callbackRunnerObject != null)
                {
                    UnityEngine.Object.Destroy(_callbackRunnerObject);
                    _callbackRunnerObject = null;
                }
                
                SteamAPI.Shutdown();
                _isInitialized.Value = false;
                Debug.Log("[Steamworks.NET] Steam API shutdown.");
            }
        }
    }
}
