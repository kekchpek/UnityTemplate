using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kekchpek.GameSaves.Data;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.Data;
using kekchpek.SaveSystem.SaveManagers;
using kekchpek.SaveSystem.Utils;
using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using kekchpek.Auxiliary.Application;
using kekchpek.Auxiliary.Time;
using kekchpek.Auxiliary.Time.Extensions;
using kekchpek.GameSaves.Codecs;
using Diagnostics.Time;
using kekchpek.Auxiliary.Configs;

namespace kekchpek.GameSaves
{
    public class GameSaveManager : IGameSaveController, IGameSaveManager
    {

        public static string SaveDataFolder = Application.persistentDataPath + "/GameSaves/";

        private readonly Mutable<bool> _isInitialized = new(false);
        private const string SelectedProfileKey = "SelectedProfile";

        private readonly Dictionary<string, StreamSaveManager> _exclusiveDataProviders = new();

        private MultifileSaveManager _gameSaveManager;
        private StreamSaveManager _settingsSaveManager;
        private StreamSaveManager _commonDataSaveManager;
        private readonly IApplicationService _applicationService;
        private readonly IConfigsProvider _configsProvider;
        private IMutable<string> _selectedProfile;
        private string _resolvedCommonDataPath;
        private GameSavesConfig _config;

#if UNITY_WEBGL
        private const bool UseSaveMultithreading = false;
#else
        private const bool UseSaveMultithreading = true;
#endif

        private readonly Dictionary<Type, Action<StreamSaveManager>> _registerCodecActions = new();
        private readonly IMutableFactory _mutableFactory;
        private readonly ITimeManager _timeManager;

        private bool _autosaveEnabled;
        private long _autosaveIntervalMs;

        public IBindable<bool> IsInitialized => _isInitialized;

        public GameSaveManager(
            IApplicationService applicationService,
            IConfigsProvider configsProvider,
            ITimeManager timeManager,
            IMutableFactory mutableFactory = null)
        {
            _applicationService = applicationService;
            _configsProvider = configsProvider;
            _timeManager = timeManager;
            _mutableFactory = mutableFactory ?? DefaultMutableFactory.Instance;
        }

        public UniTask Initialize() {
            using (TimeDebug.StartMeasure("GameSaveManager.Initialize"))
            {
                Debug.Log("[GameSaveManager] Initializing save system...");
                _config = _configsProvider.GetConfig<GameSavesConfig>();
                StaticBufferPool.Prewarm(EnumeratePrewarmSpecs(_config.PrewarmedBuffers));
                var savePath = SaveDataFolder + _config.DataFolder;
                _resolvedCommonDataPath = savePath;
                var gameSavePath = savePath + "/" + _config.SaveFolder;
                
                Debug.Log($"[GameSaveManager] Setting up save paths:\n" +
                        $"Game saves: {gameSavePath}\n" +
                        $"Settings and common: {savePath}");
                
                var mutableFactory = _mutableFactory;
                _gameSaveManager = new MultifileSaveManager(gameSavePath, _config.CustomCodecsVersion, mutableFactory);
                _settingsSaveManager = new FileSaveManager(savePath, _config.CustomCodecsVersion, true, UseSaveMultithreading, mutableFactory);
                _commonDataSaveManager = new FileSaveManager(savePath, _config.CustomCodecsVersion, true, UseSaveMultithreading, mutableFactory);
                _settingsSaveManager.LoadOrCreate(_config.SettingsSaveFile);
                _commonDataSaveManager.LoadOrCreate(_config.CommonSaveFile);

                _commonDataSaveManager.SaveOnChangesDebounceMs = (int)_config.CommonDataDebounceIntervalMs;
                _commonDataSaveManager.MaxSaveOnChangesTimeMs = _config.MaxSaveOnChangesTimeMs;
                _settingsSaveManager.SaveOnChangesDebounceMs = (int)_config.SettingsDebounceIntervalMs;
                _settingsSaveManager.MaxSaveOnChangesTimeMs = _config.MaxSaveOnChangesTimeMs;

                // Saving is driven by the autosave timer below, not by per-change debouncing.
                _settingsSaveManager.SaveOnChangesEnabled = false;
                _commonDataSaveManager.SaveOnChangesEnabled = false;
                _gameSaveManager.SaveOnChangesEnabled = false;

                RegisterCodecs();

                _selectedProfile = _commonDataSaveManager.DeserializeAndCaptureCustomValue<string>(SelectedProfileKey, () => null);
                RefreshSelectedProfileInternal();

                _applicationService.ApplicationQuit += OnApplicationQuit;
#if UNITY_WEBGL && !UNITY_EDITOR
                Application.focusChanged += OnApplicationFocusChanged;
#endif
                _isInitialized.Value = true;
                ToggleAutosave(_config.AutosaveEnabled, _config.AutosaveIntervalMs);
                return UniTask.CompletedTask;
            }
        }

        private void Autosave()
        {
            try
            {
                SaveAllExplicitly();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveManager] Autosave failed: {e.Message}\n{e.StackTrace}");
            }

            if (_autosaveEnabled)
                _timeManager.AddCallbackIn(_autosaveIntervalMs * TimeSpan.TicksPerMillisecond, Autosave);
        }

        public void ToggleAutosave(bool enabled, long autosaveIntervalMs)
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot toggle autosave before initialization is completed.");
                return;
            }

            _autosaveEnabled = enabled;
            _autosaveIntervalMs = autosaveIntervalMs;
            if (enabled)
                _timeManager.AddCallbackIn(autosaveIntervalMs * TimeSpan.TicksPerMillisecond, Autosave);
        }

        private static IEnumerable<(int size, int elementSize, int count)> EnumeratePrewarmSpecs(
            IReadOnlyList<PrewarmBufferEntry> entries)
        {
            foreach (var entry in entries)
            {
                yield return (entry.Size, entry.ElementSize, entry.Count);
            }
        }

        public void RefreshSelectedProfile() 
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot refresh selected profile before initialization is completed.");
                return;
            }
            RefreshSelectedProfileInternal();
        }

        private void RefreshSelectedProfileInternal()
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
            RegisterCustomCodec(new StringMutableListCodec());
            RegisterCustomCodec(new StringArrayCodec());
            RegisterCustomCodec(new StringHashSetCodec());
            RegisterCustomCodec(new ByteArrayCodec());
            RegisterCustomCodec(new BigIntegerCodec());
        }

        private void OnApplicationQuit()
        {
            FlushAllSaves("Application quitting");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus || !_isInitialized.Value)
                return;

            FlushAllSaves("Application lost focus");
        }
#endif

        private void FlushAllSaves(string reason)
        {
            Debug.Log($"[GameSaveManager] {reason}, performing final saves...");
            SaveAllExplicitly();
        }

        private byte[] SaveAllExplicitly()
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot save explicitly before initialization is completed.");
                return null;
            }

            try
            {
                byte[] gameSaveHash = null;
                if (_selectedProfile.Value != null)
                {
                    gameSaveHash = _gameSaveManager.SaveExplicitly();
                }

                _commonDataSaveManager.SaveExplicitly();
                _settingsSaveManager.SaveExplicitly();

                foreach (var provider in _exclusiveDataProviders)
                {
                    provider.Value.SaveExplicitly();
                }

                return gameSaveHash;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveManager] Error during save: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        string IGameSaveController.CurrentSaveId => _gameSaveManager.CurrentSaveId;

        public IMultifileSaveDataProvider GameDataProvider
        {
            get
            {
                if (!_isInitialized.Value)
                {
                    Debug.LogError("Cannot get game data provider before initialization is completed.");
                    return null;
                }
                return _gameSaveManager;
            }
        }

        public ISaveDataProvider SettingsDataProvider
        {
            get
            {
                if (!_isInitialized.Value)
                {
                    Debug.LogError("Cannot get settings data provider before initialization is completed.");
                    return null;
                }
                return _settingsSaveManager;
            }
        }

        byte[] IGameSaveController.SaveExplicitly()
        {
            return SaveAllExplicitly();
        }

        void IGameSaveController.ToggleAutosave(bool enabled, long autosaveIntervalMs)
        {
            ToggleAutosave(enabled, autosaveIntervalMs);
        }

        string[] IGameSaveController.GetSaveIds()
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot get save ids before initialization is completed.");
                return new string[0];
            }
            return _gameSaveManager.GetSaves();
        }

        void IGameSaveController.LoadOrCreate(string saveId) {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot load or create save before initialization is completed.");
                return;
            }
            _gameSaveManager.LoadOrCreate(saveId);
            _selectedProfile.Value = saveId;
        }

        void IGameSaveController.RemoveSave(string saveId)
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot remove save before initialization is completed.");
                return;
            }
            _gameSaveManager.RemoveSave(saveId);
            if (_selectedProfile.Value == saveId)
            {
                _selectedProfile.Value = null;
            }
        }

        async UniTask<IReadOnlyList<T>> IGameSaveController.GetSaves<T>()
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot get saves before initialization is completed.");
                return new List<T>();
            }
            var saves = _gameSaveManager.GetSaves();
            var loadTasks = new Task<IDataContainer>[saves.Length];
            for (var i = 0; i < saves.Length; i++)
            {
                var saveId = saves[i];
                loadTasks[i] = _gameSaveManager.GetMetaData(saveId);
            }

            await Task.WhenAll(loadTasks);
            var outcome = UnityEngine.Pool.ListPool<T>.Get();
            for (var i = 0; i < loadTasks.Length; i++)
            {
                var dataContainer = loadTasks[i].Result;
                var saveData = new T();
                saveData.SetDataContainer(dataContainer);
                outcome.Add(saveData);
            }

            return outcome;
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            if (_registerCodecActions.ContainsKey(typeof(T)))
            {
                Debug.LogError($"Custom codec for type {typeof(T)} is already registered.");
                return;
            }
            _registerCodecActions.Add(typeof(T), (saveManager) => saveManager.RegisterCustomCodec(codec));
            _gameSaveManager.RegisterCustomCodec(codec);
            _settingsSaveManager.RegisterCustomCodec(codec);
            _commonDataSaveManager.RegisterCustomCodec(codec);
            foreach (var provider in _exclusiveDataProviders)
            {
                provider.Value.RegisterCustomCodec(codec);
            }
        }

        public void UnregisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            _gameSaveManager.UnregisterCustomCodec(codec);
            _settingsSaveManager.UnregisterCustomCodec(codec);
            _commonDataSaveManager.UnregisterCustomCodec(codec);
            _registerCodecActions.Remove(typeof(T));
            foreach (var provider in _exclusiveDataProviders)
            {
                provider.Value.UnregisterCustomCodec(codec);
            }
        }

        public ICustomCodec<T> GetCodec<T>()
        {
            return _gameSaveManager.GetCustomCodec<T>();
        }

        public ISaveDataProvider GetExclusiveDataProvider(string dataName)
        {
            if (!_isInitialized.Value)
            {
                Debug.LogError("Cannot get exclusive data provider before initialization is completed.");
                return null;
            }
            if (_exclusiveDataProviders.TryGetValue(dataName, out var provider))
            {
                return provider;
            }
            var newProvider = new FileSaveManager(
                _resolvedCommonDataPath,
                _config.CustomCodecsVersion,
                UseSaveMultithreading,
                true,
                _mutableFactory);
            foreach (var action in _registerCodecActions)
            {
                action.Value(newProvider);
            }
            newProvider.LoadOrCreate(dataName);
            _exclusiveDataProviders.Add(dataName, newProvider);
            return newProvider;
        }
    }
    
}