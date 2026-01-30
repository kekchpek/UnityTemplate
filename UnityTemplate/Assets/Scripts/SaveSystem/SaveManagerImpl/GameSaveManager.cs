using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kekchpek.GameSaves.Data;
using kekchpek.GameSaves.Static;
using AsyncReactAwait.Bindable;
using kekchpek.GameSaves.Codecs;
using Newtonsoft.Json;
using kekchpek.SaveSystem;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.Data;
using kekchpek.SaveSystem.SaveManagers;
using kekchpek.SaveSystem.Utils;
using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using kekchpek.Auxiliary.Application;
using AssetsSystem;
using kekchpek.Auxiliary.Time;
using kekchpek.Auxiliary.Time.Extensions;

namespace kekchpek.GameSaves
{
    public class GameSaveManager : IGameSaveController, IGameSaveManager
    {
        private readonly Mutable<bool> _isInitialized = new(false);
        private const string SelectedProfileKey = "SelectedProfile";
        private const string SaveConfigPath = "Configs/SaveConfig";

        private readonly Dictionary<string, BaseSaveManager> _exclusiveDataProviders = new();

        private BaseSaveManager _gameSaveManager;
        private BaseSaveManager _settingsSaveManager;
        private BaseSaveManager _commonDataSaveManager;
        private readonly IApplicationService _applicationService;
        private readonly IAssetsModel _assetsModel;
        private readonly ITimeManager _timeManager;

        private SaveConfig _config;

        private IMutable<string> _selectedProfile;

        public IBindable<bool> IsInitialized => _isInitialized;

        public GameSaveManager(
            IApplicationService applicationService,
            IAssetsModel assetsModel,
            ITimeManager timeManager) {
            _applicationService = applicationService;
            _assetsModel = assetsModel;
            _timeManager = timeManager;
        }

        public async UniTask Initialize() {
            Debug.Log("[GameSaveManager] Initializing save system...");
            var configJson = await _assetsModel.LoadAsset<TextAsset>(SaveConfigPath);
            _config = JsonConvert.DeserializeObject<SaveConfig>(configJson.text);
            StaticBufferPool.Prewarm(_config.PrewarmedBuffers);
            
            var savePath = Application.persistentDataPath + "/" + _config.DataFolder;
            var gameSavePath = savePath + "/" + _config.SaveFolder;
            
            Debug.Log($"[GameSaveManager] Setting up save paths:\n" +
                     $"Game saves: {gameSavePath}\n" +
                     $"Settings and common: {savePath}");
            
            _gameSaveManager = new FileSaveManager(gameSavePath);
            _settingsSaveManager = new FileSaveManager(savePath);
            _commonDataSaveManager = new FileSaveManager(savePath);

            _assetsModel.ReleaseLoadedAssets(SaveConfigPath);
            _settingsSaveManager.LoadOrCreate(_config.SettingsSaveFile);
            _commonDataSaveManager.LoadOrCreate(_config.CommonSaveFile);
            _commonDataSaveManager.SaveOnChangesDebounceMs = (int)_config.CommonDataDebounceIntervalMs;
            _commonDataSaveManager.MaxSaveOnChangesTimeMs = 100000000;
            _settingsSaveManager.SaveOnChangesDebounceMs = (int)_config.SettingsDebounceIntervalMs;
            _settingsSaveManager.MaxSaveOnChangesTimeMs = 100000000;

            _settingsSaveManager.SaveOnChangesEnabled = true;
            _commonDataSaveManager.SaveOnChangesEnabled = true;
            _gameSaveManager.SaveOnChangesEnabled = false;


            _selectedProfile = _commonDataSaveManager.DeserializeAndCaptureCustomValue<string>(SelectedProfileKey, () => null);
            RefreshSelectedProfile();

            RegisterCodecs();

            // Launches the autosave
            if (_config.AutosaveEnabled)
                Autosave();

            _isInitialized.Value = true;
        }
        
        public void RefreshSelectedProfile() 
        {
            if (_selectedProfile.Value != null) {
                Debug.Log($"[GameSaveManager] Loading existing profile: {_selectedProfile.Value}");
                _gameSaveManager.LoadOrCreate(_selectedProfile.Value);
            } else {
                // If no profile is selected, create a default one to ensure proper initialization
                const string defaultProfile = "default_save";
                Debug.Log($"[GameSaveManager] No profile selected, creating default profile: {defaultProfile}");
                _gameSaveManager.LoadOrCreate(defaultProfile);
                _selectedProfile.Value = defaultProfile;
            }
        }

        private void RegisterCodecs()
        {
            _gameSaveManager.RegisterCustomCodec(new StringListCodec());
            _gameSaveManager.RegisterCustomCodec(new StringArrayCodec());

            _applicationService.ApplicationQuit += OnApplicationQuit;
        }

        private void OnApplicationQuit()
        {
            Debug.Log("[GameSaveManager] Application quitting, performing final saves...");
            try
            {
                if (_selectedProfile.Value != null)
                {
                    Debug.Log($"[GameSaveManager] Saving game data for profile: {_selectedProfile.Value}");
                    _gameSaveManager.SaveExplicitly();
                }
                
                Debug.Log("[GameSaveManager] Saving common data...");
                _commonDataSaveManager.SaveExplicitly();
                Debug.Log("[GameSaveManager] All saves completed successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveManager] Error during final save: {e.Message}\n{e.StackTrace}");
            }
        }

        private void Autosave() {
            if (_selectedProfile.Value != null) 
            {
                try 
                {
                    Debug.Log($"[GameSaveManager] Starting autosave for profile: {_selectedProfile.Value}");
                    _gameSaveManager.SaveExplicitly();
                    Debug.Log($"[GameSaveManager] Autosave completed successfully for profile: {_selectedProfile.Value}");
                }
                catch (Exception e) 
                {
                    Debug.LogError($"[GameSaveManager] Autosave failed for profile {_selectedProfile.Value}: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning("[GameSaveManager] Skipping autosave - no profile selected");
            }
            _timeManager.AddCallbackIn(_config.AutosaveIntervalMs * TimeSpan.TicksPerMillisecond, Autosave);
        }

        string IGameSaveController.CurrentSaveId => _gameSaveManager.CurrentSaveId;

        public ISaveDataProvider GameDataProvider => _gameSaveManager;

        public ISaveDataProvider SettingsDataProvider => _settingsSaveManager;

        void IGameSaveController.SaveExplicitly() => _gameSaveManager.SaveExplicitly();
        string[] IGameSaveController.GetSaveIds() => _gameSaveManager.GetSaves();

        void IGameSaveController.LoadOrCreate(string saveId) {

            _gameSaveManager.LoadOrCreate(saveId);
            _selectedProfile.Value = saveId;
        }
        
        async UniTask<IReadOnlyList<SaveData>> IGameSaveController.GetSaves()
        {
            var saves = _gameSaveManager.GetSaves();
            var loadTasks = new Task<IDataContainer>[saves.Length];
            for (var i = 0; i < saves.Length; i++)
            {
                var saveId = saves[i];
                loadTasks[i] = _gameSaveManager.GetMetaData(saveId);
            }

            await Task.WhenAll(loadTasks);
            var outcome = ListPool<SaveData>.Get();
            for (var i = 0; i < loadTasks.Length; i++)
            {
                var dataContainer = loadTasks[i].Result;
                outcome.Add(new SaveData(
                    saves[i],
                    dataContainer.DeserializeStructValue(DataNames.Level, false, 1),
                    1 // Default day for now
                ));
            }

            return outcome;
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            _gameSaveManager.RegisterCustomCodec(codec);
            _settingsSaveManager.RegisterCustomCodec(codec);
            _commonDataSaveManager.RegisterCustomCodec(codec);
        }

        public ICustomCodec<T> GetCodec<T>()
        {
            return _gameSaveManager.GetCustomCodec<T>();
        }

        public ISaveDataProvider GetExclusiveDataProvider(string dataName)
        {
            if (_exclusiveDataProviders.TryGetValue(dataName, out var provider))
            {
                return provider;
            }
            var newProvider = new FileSaveManager(Application.persistentDataPath + "/" + _config.DataFolder);
            newProvider.LoadOrCreate(dataName);
            _exclusiveDataProviders.Add(dataName, newProvider);
            return newProvider;
        }
    }
    
}