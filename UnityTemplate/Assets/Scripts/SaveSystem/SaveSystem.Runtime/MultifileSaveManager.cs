using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.SaveManagers;
using UnityEngine;

namespace kekchpek.SaveSystem
{
    public class MultifileSaveManager : ISaveManager, IMultifileSaveDataProvider
    {

        private const string DefaultDataFileName = "defaultData";
        private const string IntegrityHashFileName = "integrityhash";
        private const string FallbackSaveSuffix = "___fallbacksave";
        private const int IntegrityHashSizeBytes = 32;

        private readonly SortedDictionary<string, StreamSaveManager> _saveManagers = new();
        private readonly Dictionary<Type, Action<StreamSaveManager>> _registerCodecActions = new();
        private readonly string _folderPath;
        private readonly int _currentCustomCodecsVersion;
        private readonly IMutableFactory _mutableFactory;
        public string CurrentSaveId { get; private set; }

        private bool _saveOnChangesEnabled;
        private int _saveOnChangesDebounceMs = 5000;
        private int _maxSaveOnChangesTimeMs = 20000;

        /// <inheritdoc />
        /// <remarks>Propagated to every per-file save manager, including ones created later.</remarks>
        public bool SaveOnChangesEnabled
        {
            get => _saveOnChangesEnabled;
            set
            {
                _saveOnChangesEnabled = value;
                foreach (var saveManager in _saveManagers.Values)
                    saveManager.SaveOnChangesEnabled = value;
            }
        }

        /// <inheritdoc />
        public int SaveOnChangesDebounceMs
        {
            get => _saveOnChangesDebounceMs;
            set
            {
                _saveOnChangesDebounceMs = value;
                foreach (var saveManager in _saveManagers.Values)
                    saveManager.SaveOnChangesDebounceMs = value;
            }
        }

        /// <inheritdoc />
        public int MaxSaveOnChangesTimeMs
        {
            get => _maxSaveOnChangesTimeMs;
            set
            {
                _maxSaveOnChangesTimeMs = value;
                foreach (var saveManager in _saveManagers.Values)
                    saveManager.MaxSaveOnChangesTimeMs = value;
            }
        }

        public MultifileSaveManager(
            string folderPath,
            int currentCustomCodecsVersion,
            IMutableFactory mutableFactory = null)
        {
            _folderPath = folderPath;
            _currentCustomCodecsVersion = currentCustomCodecsVersion;
            _mutableFactory = mutableFactory ?? DefaultMutableFactory.Instance;
        }

        public IMutable<T> DeserializeAndCaptureCustomValue<T>(
            string valueKey, Func<T> defaultValueFactory = null, 
            bool isMetaValue = false)
        {
            var saveManager = GetOrCreateSaveManager(valueKey);
            return saveManager.DeserializeAndCaptureCustomValue(valueKey, defaultValueFactory, isMetaValue);
        }

        public T DeserializeAndCaptureSavableObject<T>(
            string valueKey, Func<T> factoryMethod = null, 
            bool isMetaValue = false) where T : ISaveObject, new()
        {
            var saveManager = GetOrCreateSaveManager(valueKey);
            return saveManager.DeserializeAndCaptureSavableObject(valueKey, factoryMethod, isMetaValue);
        }

        public IMutable<T> DeserializeAndCaptureStructValue<T>(
            string valueKey, T defaultValue = default, 
            bool isMetaValue = false) where T : unmanaged
        {
            var saveManager = GetOrCreateSaveManager(valueKey);
            return saveManager.DeserializeAndCaptureStructValue(valueKey, defaultValue, isMetaValue);
        }

        public async Task<IDataContainer> GetMetaData(string saveId)
        {
            var dataContainerAggregator = new DataContainerAggregator();
            foreach (var saveManager in _saveManagers)
            {
                dataContainerAggregator.AddContainer(saveManager.Key, await saveManager.Value.GetMetaData(saveId));
            }
            return dataContainerAggregator;
        }

        public string[] GetSaves()
        {
            return Directory.GetDirectories(_folderPath);
        }

        public void RemoveSave(string saveId)
        {
            RemoveProfileDirectory(GetProfilePath(saveId));
            RemoveProfileDirectory(GetFallbackProfilePath(saveId));

            if (CurrentSaveId == saveId)
            {
                CurrentSaveId = null;
                _saveManagers.Clear();
            }
        }

        public void LoadOrCreate(string saveId)
        {
            CurrentSaveId = saveId;
            _saveManagers.Clear();
            TryRestoreFromFallback(saveId);

            var profilePath = GetProfilePath(saveId);
            if (!Directory.Exists(profilePath))
            {
                Directory.CreateDirectory(profilePath);
            }

            var defaultSaveManager = CreateFileSaveManager(profilePath, _currentCustomCodecsVersion);
            AddSaveManager(DefaultDataFileName, defaultSaveManager);
            defaultSaveManager.LoadOrCreate(DefaultDataFileName);
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            var type = typeof(T);
            if (_registerCodecActions.ContainsKey(type))
            {
                Debug.LogError($"Custom codec for type {type} is already registered.");
                return;
            }

            _registerCodecActions.Add(type, saveManager => saveManager.RegisterCustomCodec(codec));
            foreach (var saveManager in _saveManagers.Values)
            {
                saveManager.RegisterCustomCodec(codec);
            }
        }

        public void UnregisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            _registerCodecActions.Remove(typeof(T));
            foreach (var saveManager in _saveManagers.Values)
            {
                saveManager.UnregisterCustomCodec(codec);
            }
        }

        public ICustomCodec<T> GetCustomCodec<T>()
        {
            return _saveManagers.TryGetValue(DefaultDataFileName, out var saveManager)
                ? saveManager.GetCustomCodec<T>()
                : null;
        }

        private void AddSaveManager(string fileName, StreamSaveManager saveManager)
        {
            foreach (var registerCodec in _registerCodecActions.Values)
            {
                registerCodec(saveManager);
            }

            _saveManagers.Add(fileName, saveManager);
        }

        private StreamSaveManager GetOrCreateSaveManager(string dataPath)
        {
            var parts = dataPath.Split(':');
            string fileName;
            if (parts.Length == 1)
            {
                fileName = DefaultDataFileName;
            }
            else if (parts.Length == 2)
            {
                fileName = parts[0];
            }
            else
            {
                Debug.LogError($"Onlyt one ':' is allowed in the data path. Data path: {dataPath}");
                fileName = DefaultDataFileName;
            }
            if (!_saveManagers.TryGetValue(fileName, out var saveManager))
            {
                saveManager = CreateFileSaveManager(GetProfilePath(CurrentSaveId), _currentCustomCodecsVersion);
                saveManager.LoadOrCreate(fileName);
                AddSaveManager(fileName, saveManager);
            }
            return saveManager;

        }

        private FileSaveManager CreateFileSaveManager(string profilePath, int currentCustomCodecsVersion)
        {
            return new FileSaveManager(profilePath, currentCustomCodecsVersion, false, false, _mutableFactory)
            {
                SaveOnChangesEnabled = _saveOnChangesEnabled,
                SaveOnChangesDebounceMs = _saveOnChangesDebounceMs,
                MaxSaveOnChangesTimeMs = _maxSaveOnChangesTimeMs,
            };
        }

        private string GetProfilePath(string saveId)
        {
            return Path.Combine(_folderPath, saveId);
        }

        private string GetFallbackProfilePath(string saveId)
        {
            return Path.Combine(_folderPath, saveId + FallbackSaveSuffix);
        }

        private void TryRestoreFromFallback(string saveId)
        {
            var primaryPath = GetProfilePath(saveId);
            if (ValidateProfile(primaryPath))
            {
                return;
            }

            var fallbackPath = GetFallbackProfilePath(saveId);
            if (!ValidateProfile(fallbackPath))
            {
                return;
            }

            Debug.LogWarning($"Profile '{saveId}' is corrupted. Restoring from fallback.");
            CopyProfileDirectory(fallbackPath, primaryPath);
        }

        private bool ValidateProfile(string profilePath)
        {
            if (!TryReadIntegrityHash(profilePath, out var storedHash))
            {
                return false;
            }

            var fileNames = CollectDataFileNames(profilePath);
            if (fileNames.Count == 0)
            {
                return false;
            }

            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            for (var i = 0; i < fileNames.Count; i++)
            {
                if (!StreamSaveManager.TryGetValidatedFileHash(Path.Combine(profilePath, fileNames[i]), out var fileHash))
                {
                    return false;
                }

                incrementalHash.AppendData(fileHash);
            }

            return CryptographicOperations.FixedTimeEquals(incrementalHash.GetHashAndReset(), storedHash);
        }

        private static List<string> CollectDataFileNames(string profilePath)
        {
            var files = Directory.GetFiles(profilePath);
            var fileNames = new List<string>(files.Length);
            for (var i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileName(files[i]);
                if (fileName != IntegrityHashFileName)
                {
                    fileNames.Add(fileName);
                }
            }

            fileNames.Sort(StringComparer.Ordinal);
            return fileNames;
        }

        public byte[] SaveExplicitly()
        {
            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var saveManager in _saveManagers)
            {
                var hash = saveManager.Value.SaveExplicitly();
                if (hash != null)
                {
                    incrementalHash.AppendData(hash);
                }
                else
                {
                    return null;
                }
            }
            var integrityHash = incrementalHash.GetHashAndReset();
            var integrityHashPath = Path.Combine(_folderPath, CurrentSaveId, IntegrityHashFileName);
            File.WriteAllBytes(integrityHashPath, integrityHash);

            byte[] storedHash;
            try
            {
                storedHash = File.ReadAllBytes(integrityHashPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Fail to read back integrity hash after save: {e.Message}");
                Debug.LogException(e);
                return null;
            }

            if (storedHash.Length != IntegrityHashSizeBytes ||
                !CryptographicOperations.FixedTimeEquals(storedHash, integrityHash))
            {
                Debug.LogError("Integrity hash read-back verification failed.");
                return null;
            }

            CreateFallback();

            return integrityHash;
        }

        private void CreateFallback()
        {
            var primaryPath = GetProfilePath(CurrentSaveId);
            if (!Directory.Exists(primaryPath))
            {
                Debug.LogError($"Fail to create fallback save. Profile folder {primaryPath} not found.");
                return;
            }

            CopyProfileDirectory(primaryPath, GetFallbackProfilePath(CurrentSaveId));
        }

        private static void CopyProfileDirectory(string sourcePath, string destinationPath)
        {
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, recursive: true);
            }

            Directory.CreateDirectory(destinationPath);
            var files = Directory.GetFiles(sourcePath);
            for (var i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileName(files[i]);
                File.Copy(files[i], Path.Combine(destinationPath, fileName), overwrite: true);
            }
        }

        private static void RemoveProfileDirectory(string profilePath)
        {
            if (!Directory.Exists(profilePath))
            {
                return;
            }

            try
            {
                Directory.Delete(profilePath, recursive: true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error removing profile at {profilePath}: {e.Message}");
                Debug.LogException(e);
            }
        }

        private static bool TryReadIntegrityHash(string profilePath, out byte[] hash)
        {
            hash = null;
            var integrityHashPath = Path.Combine(profilePath, IntegrityHashFileName);
            if (!File.Exists(integrityHashPath))
            {
                return false;
            }

            try
            {
                hash = File.ReadAllBytes(integrityHashPath);
                return hash.Length == IntegrityHashSizeBytes;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading integrity hash at {integrityHashPath}: {e.Message}");
                return false;
            }
        }

        public void UseExplicitSaveSystemVersion(int version)
        {
            // no version diffrerences for now
        }

        public byte[] GetSaveHash(string saveId)
        {
            TryReadIntegrityHash(GetProfilePath(saveId), out var hash);
            return hash;
        }

        public void ReleaseFile(string fileName, bool saveBeforeRelease = true)
        {
            if (saveBeforeRelease)
            {
                SaveExplicitly();
            }
            _saveManagers.Remove(fileName);
        }
    }
}