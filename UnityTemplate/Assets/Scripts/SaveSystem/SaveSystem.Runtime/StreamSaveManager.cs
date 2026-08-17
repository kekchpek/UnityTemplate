using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;
using kekchpek.SaveSystem.Utils;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace kekchpek.SaveSystem
{
    public abstract class StreamSaveManager : 
        ISaveManager, 
        ICustomCodecsProvider, 
        ICustomCodecsRegister, 
        ISaveDataProvider
    {

        private const int MaxAttemptsToSave = 3;


        public string CurrentSaveId { get; private set; }

        /// <summary>
        /// When enabled, any change reported by a captured <see cref="ISaveObject"/> schedules
        /// a debounced save instead of requiring an explicit <see cref="SaveExplicitly"/> call.
        /// </summary>
        public bool SaveOnChangesEnabled { get; set; }

        /// <summary>
        /// How long to wait after the last change before writing the save.
        /// </summary>
        public int SaveOnChangesDebounceMs { get; set; } = 5000;

        /// <summary>
        /// Upper bound on how long changes may keep deferring a save. Once exceeded, the save
        /// runs immediately so a stream of small changes cannot starve persistence.
        /// </summary>
        public int MaxSaveOnChangesTimeMs { get; set; } = 20000;

        private readonly IReadOnlyList<ISaveFileCodec> _codecs;
        private int _saveSystemVersion;
        
        private readonly Dictionary<Type, SaveData> _capturedData = new();
        private readonly Dictionary<Type, SaveData> _capturedMetaData = new();

        private readonly Dictionary<Type, ICustomCodec> _customCodecs = new();

        private Dictionary<string, object> _createdMutableValues = new();
        
        private IDataContainer _loadedDataContainer;
        private IDataContainer _loadedMetaDataContainer;

        private readonly object _stateLock = new();
        private bool _loading;
        private bool _saving;
        private readonly bool _useMultithreading;
        private readonly bool _useFallback;
        private readonly IMutableFactory _mutableFactory;
        private long _lastSaveTime;
        private CancellationTokenSource _debounceSaveCts;

        protected StreamSaveManager(
            int currentCustomCodecsVersion,
            bool useMultithreading = true,
            bool useFallback = true,
            IMutableFactory mutableFactory = null)
        {
            _useMultithreading = useMultithreading;
            _useFallback = useFallback;
            _mutableFactory = mutableFactory ?? DefaultMutableFactory.Instance;
            _codecs = new ISaveFileCodec[] {
                new SaveFileCodecV0(this),
                new SaveFileCodecV1(this, currentCustomCodecsVersion),
            };
            _saveSystemVersion = _codecs.Count - 1;
            RegisterCustomCodec(new StringCodec());
        }

        public IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged
        {
            lock (_stateLock)
            {
                if (_createdMutableValues.TryGetValue(valueKey, out var existingMutableObj) &&
                    existingMutableObj is IMutable<T> existingMutable)
                {
                    return existingMutable;
                }

                var dataContainer = isMetaValue ? _loadedMetaDataContainer : _loadedDataContainer;
                if (dataContainer == null)
                {
                    Debug.LogWarning($"Data container is null when deserializing {valueKey}. Creating new container.");
                    var emptyValues = DictionaryPool<string, ILoadStream>.Get();
                    dataContainer = new SerializedDataContainer(emptyValues, this);
                    if (isMetaValue)
                        _loadedMetaDataContainer = dataContainer;
                    else
                        _loadedDataContainer = dataContainer;
                }
                var val = dataContainer.DeserializeStructValue(valueKey, true, defaultValue);
                var newVal = GetOrCreateMutableValue(valueKey, val, isMetaValue);
                return newVal;
            }
        }

        public IMutable<T> DeserializeAndCaptureCustomValue<T>(
            string valueKey, 
            Func<T> defaultValueFactory = null,
            bool isMetaValue = false)
        {

            lock (_stateLock)
            {
                if (_createdMutableValues.TryGetValue(valueKey, out var existingMutableObj) &&
                    existingMutableObj is IMutable<T> existingMutable)
                {
                    return existingMutable;
                }

                var dataContainer = isMetaValue ? _loadedMetaDataContainer : _loadedDataContainer;
                if (dataContainer == null)
                {
                    Debug.LogWarning($"Data container is null when deserializing {valueKey}. Creating new container.");
                    var emptyValues = DictionaryPool<string, ILoadStream>.Get();
                    dataContainer = new SerializedDataContainer(emptyValues, this);
                    if (isMetaValue)
                        _loadedMetaDataContainer = dataContainer;
                    else
                        _loadedDataContainer = dataContainer;
                }
                var val = dataContainer.DeserializeCustomValue(valueKey, true, defaultValueFactory);
                var mutableVal = CreateMutableCustomValue(valueKey, val, isMetaValue);
                return mutableVal;
            }
        }

        public T DeserializeAndCaptureSavableObject<T>(string valueKey, Func<T> factoryMethod = null,
            bool isMetaValue = false) where T : ISaveObject, new()
        {
            lock (_stateLock)
            {
                var capturedDataDict = isMetaValue ? _capturedMetaData : _capturedData;
                if (capturedDataDict.TryGetValue(typeof(T), out var existingCaptured)
                    && existingCaptured.DataNames.Contains(valueKey))
                {
                    var list = (List<ISaveObject>)existingCaptured.Data;
                    var existingIndex = existingCaptured.DataNames.IndexOf(valueKey);
                    return (T)list[existingIndex];
                }

                var dataContainer = isMetaValue ? _loadedMetaDataContainer : _loadedDataContainer;
                var val = dataContainer.DeserializeSavableObject(valueKey, true, factoryMethod);
                return RegisterSavableValue(valueKey, val, isMetaValue);
            }
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            var type = typeof(T);
            if (_customCodecs.ContainsKey(type)) {
                Debug.LogError($"Custom codec for type {type} is already registered.");
                return;
            }
            _customCodecs.Add(type, codec);
        }

        public void UnregisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            if (_customCodecs.TryGetValue(typeof(T), out var existingCodec) && existingCodec == codec)
            {
                _customCodecs.Remove(typeof(T));
            }
            else
            {
                Debug.LogError($"Custom codec for type {typeof(T)} is not registered or does not match the provided codec.");
            }
        }

        public void RemoveCustomCodec<T>() 
        {
            _customCodecs.Remove(typeof(T));
        }

        public byte[] SaveExplicitly()
        {
            if (_debounceSaveCts != null)
            {
                Debug.Log("Cancel debounced save. Because of explicit saving.");
                _debounceSaveCts.Cancel();
                _debounceSaveCts.Dispose();
                _debounceSaveCts = null;
            }
            return SaveInternal();
        }

        private void OnAnyValueChanged()
        {
            if (SaveOnChangesEnabled)
                DebounceSave();
        }

        private async void DebounceSave()
        {
            _debounceSaveCts?.Cancel();
            _debounceSaveCts?.Dispose();
            _debounceSaveCts = new CancellationTokenSource();
            var currentTime = Stopwatch.GetTimestamp();
            var maxSaveOnChangesTimeTicks = MaxSaveOnChangesTimeMs * (long)10000;
            if (currentTime - _lastSaveTime > maxSaveOnChangesTimeTicks)
            {
                await RunSaveInternalAsync();
                return;
            }
            try
            {
                await Task.Delay(
                    (int)Math.Min(SaveOnChangesDebounceMs, _lastSaveTime + maxSaveOnChangesTimeTicks - currentTime),
                    cancellationToken: _debounceSaveCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            _debounceSaveCts?.Dispose();
            _debounceSaveCts = null;

            await RunSaveInternalAsync();
        }

        private Task RunSaveInternalAsync()
        {
            if (_useMultithreading)
                return Task.Run(() => SaveInternal());

            SaveInternal();
            return Task.CompletedTask;
        }

        public byte[] GetSaveHash(string saveId)
        {
            Stream stream = null;
            try
            {
                if (!TryGetStreamToRead(saveId, out stream) || stream.Length == 0)
                {
                    if (!_useFallback || !TryGetFallbackStreamToRead(saveId, out stream))
                    {
                        return null;
                    }
                }

                if (!ValidatePrepareAndGetFallbackIfNeeded(ref stream, saveId, out int saveVersion))
                {
                    return null;
                }

                if (saveVersion == 0)
                {
                    return null;
                }

                var hash = new byte[HashDigestSizeBytes];
                stream.Position = stream.Length - HashDigestSizeBytes;
                if (stream.Read(hash, 0, HashDigestSizeBytes) != HashDigestSizeBytes)
                {
                    return null;
                }

                return hash;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading save hash for {saveId}: {e.Message}");
                Debug.LogException(e);
                return null;
            }
            finally
            {
                if (stream != null)
                {
                    ReleaseStream(stream);
                }
            }
        }

        private byte[] SaveInternal()
        {
            if (_saving)
            {
                return null;
            }
            
            if (CurrentSaveId == null)
            {
                Debug.LogError("Attempt to save while no save id set.");
                return null;
            }
            if (_loading)
            {
                Debug.LogError("Attempt to save while loading.");
                return null;
            }
            if (_saving)
            {
                Debug.LogError("Attempt to save while saving.");
                return null;
            }

            int attemptsToSave = MaxAttemptsToSave;
            bool success = false;
            byte[] integrityHash = null;
            while (attemptsToSave > 0 && !success)
            {
                attemptsToSave--;
                
                Stream stream = null;
                byte[] attemptHash = null;
                try
                {
                    _saving = true;
                    stream = GetStreamToWrite(CurrentSaveId);
                    var codec = _codecs[_saveSystemVersion];
                    WriteVersion(stream, _saveSystemVersion);
                    codec.Encode(stream, 
                        _capturedMetaData.Values, _loadedMetaDataContainer,
                        _capturedData.Values, _loadedDataContainer);
                    if (_saveSystemVersion > 0)
                    {
                        attemptHash = AppendIntegrityHash(stream);
                    }
                }
                catch (Exception e)
                {
                    if (attemptsToSave > 0)
                    {
                        Debug.LogWarning($"Error saving save {CurrentSaveId}: {e.Message}");
                    }
                    else
                    {
                        Debug.LogError($"Error saving save {CurrentSaveId}: {e.Message}");
                    }

                    Debug.LogException(e);
                }
                finally
                {
                    Stream verifyStream = null;
                    try
                    {
                        if (stream != null)
                        {
                            ReleaseStream(stream);
                        }
                        if (!TryGetStreamToRead(CurrentSaveId, out verifyStream) || 
                            !ValidateAndPrepareSave(verifyStream, out _)) 
                        {
                            if (attemptsToSave > 0)
                            {
                                Debug.LogWarning($"Fail to save. Attempt to save again. ({attemptsToSave})");
                            }
                        }
                        else 
                        {
                            success = true;
                            _saving = false;
                            if (_useFallback)
                            {
                                FallbackSave(CurrentSaveId);
                            }
                            integrityHash = attemptHash;
                            _lastSaveTime = Stopwatch.GetTimestamp();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error releasing streams: {e.Message}");
                        Debug.LogException(e);
                    }
                    finally
                    {
                        if (verifyStream != null)
                        {
                            ReleaseStream(verifyStream);
                        }
                    }
                }
            }

            if (!success)
            {
                _saving = false;
                Debug.LogError($"Failed to save '{CurrentSaveId}' after {MaxAttemptsToSave} attempts.");
                return null;
            }

            return integrityHash;
        }

        protected abstract Stream GetStreamToWrite(string saveId);

        protected abstract void FallbackSave(string saveId);

        public void LoadOrCreate(string saveId)
        {
            if (_loading)
            {
                Debug.LogError("Attempt to load while loading other save.");
                return;
            }

            Stream s = null;
            try
            {
                CurrentSaveId = saveId;
                _loading = true;
                _loadedDataContainer?.Dispose();
                _loadedMetaDataContainer?.Dispose();
                foreach (var (_, val) in _capturedData)
                {
                    if (val.Data is NativeList nativeList)
                    {
                        StaticBufferPool.Release(nativeList);
                    }
                }
                _capturedData.Clear();
                _createdMutableValues.Clear();
                foreach (var (_, val) in _capturedMetaData)
                {
                    if (val.Data is NativeList nativeList)
                    {
                        StaticBufferPool.Release(nativeList);
                    }
                }
                _capturedMetaData.Clear();
                if (!TryGetStreamToRead(saveId, out s) || s.Length == 0) 
                {
                    if (!_useFallback || !TryGetFallbackStreamToRead(saveId, out s))
                    {
                        var emptyValues = DictionaryPool<string, ILoadStream>.Get();
                        _loadedDataContainer = new SerializedDataContainer(emptyValues, this);
                        _loadedMetaDataContainer = new SerializedDataContainer(emptyValues, this);
                        return;
                    }
                }
                if (!ValidatePrepareAndGetFallbackIfNeeded(ref s, saveId, out int saveVersion))
                {
                    Debug.LogError("Fail to validate and prepare save.");
                    return;
                }
                
                using Stream payloadStream = CreatePayloadStream(s, saveVersion);
                var codec = GetCodec(saveVersion);
                if (codec == null)
                    return;
                
                var values = DictionaryPool<string, ILoadStream>.Get();
                var metaValues = DictionaryPool<string, ILoadStream>.Get();
                foreach (var (key, val, isMeta) in codec.Decode(payloadStream))
                {
                    if (isMeta)
                    {
                        if (metaValues.ContainsKey(key))
                        {
                            Debug.LogWarning($"Duplicate key found in save file (meta): {key}. Skipping duplicate entry.");
                            continue;
                        }
                        metaValues.Add(key, val);
                    }
                    else
                    {
                        if (values.ContainsKey(key))
                        {
                            Debug.LogWarning($"Duplicate key found in save file: {key}. Skipping duplicate entry.");
                            continue;
                        }
                        values.Add(key, val);
                    }
                }

                _loadedDataContainer = new SerializedDataContainer(values, this);
                _loadedMetaDataContainer = new SerializedDataContainer(metaValues, this);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading save {saveId}: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                if (s != null)
                {
                    ReleaseStream(s);
                }
                _loading = false;
            }
        }

        /// <summary>
        /// The save stream ends with a hash digest block. All preceding bytes are hashed;
        /// the trailing block must equal that digest (SHA-256, <see cref="HashDigestSizeBytes"/> bytes).
        /// </summary>
        private const int HashDigestSizeBytes = 32;

        private static bool ValidateAndPrepareSave(Stream s, out int saveVersion)
        {
            saveVersion = ReadVersion(s);
            if (saveVersion < 0)
            {
                return false;
            }
            if (saveVersion == 0)
            {
                return true;
            }
            if (s.Length < HashDigestSizeBytes)
            {
                return false;
            }
            long initialPosition = s.Position;

            long hashedByteCount = s.Length - HashDigestSizeBytes;
            s.Position = 0;

            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[65536];
            long remaining = hashedByteCount;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min(buffer.Length, remaining);
                int read = s.Read(buffer, 0, chunk);
                if (read != chunk)
                {
                    return false;
                }

                incrementalHash.AppendData(buffer.AsSpan(0, read));
                remaining -= read;
            }

            byte[] computed = incrementalHash.GetHashAndReset();
            var stored = new byte[HashDigestSizeBytes];
            if (s.Read(stored, 0, HashDigestSizeBytes) != HashDigestSizeBytes)
            {
                return false;
            }
            s.Position = initialPosition;
            return CryptographicOperations.FixedTimeEquals(computed, stored);
        }

        internal static bool TryGetValidatedFileHash(string filePath, out byte[] hash)
        {
            hash = null;
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length == 0 || !ValidateAndPrepareSave(stream, out int saveVersion) || saveVersion == 0)
                {
                    return false;
                }

                hash = new byte[HashDigestSizeBytes];
                stream.Position = stream.Length - HashDigestSizeBytes;
                return stream.Read(hash, 0, HashDigestSizeBytes) == HashDigestSizeBytes;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error validating save file hash at {filePath}: {e.Message}");
                return false;
            }
        }

        private static byte[] AppendIntegrityHash(Stream stream)
        {
            long payloadLength = stream.Position;
            stream.Position = 0;

            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[65536];
            long remaining = payloadLength;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min(buffer.Length, remaining);
                int read = stream.Read(buffer, 0, chunk);
                if (read != chunk)
                {
                    throw new InvalidOperationException("Unexpected end of stream while hashing save payload.");
                }

                incrementalHash.AppendData(buffer.AsSpan(0, read));
                remaining -= read;
            }

            byte[] hash = incrementalHash.GetHashAndReset();
            stream.Position = payloadLength;
            stream.Write(hash, 0, hash.Length);
            return hash;
        }

        private static Stream CreatePayloadStream(Stream validatedSaveStream, int saveVersion)
        {
            if (saveVersion == 0)
            {
                // Wrap it anyway to prevent original stream disposing.
                // If just retrun original stream here, it will be disposed by the caller.
                // And base save manager is not suppposed to dispose stream it receives from 
                // different implementations.
                return new IntegrityPayloadStream(validatedSaveStream, 0);
            }
            return new IntegrityPayloadStream(validatedSaveStream, HashDigestSizeBytes);
        }

        protected abstract void ReleaseStream(Stream s);

        private ISaveFileCodec GetCodec(int saveVersion)
        {
            if (saveVersion < _codecs.Count)
            {
                return _codecs[saveVersion];
            }
            Debug.LogError($"Can not load file with version {saveVersion}. No suitable codec.");
            return null;
        }

        public Task<IDataContainer> GetMetaData(string saveId)
        {
            if (_useMultithreading)
            {
                return Task.Run(() =>
                {
                    lock (_stateLock)
                    {
                        return GetMetaInternal(saveId);
                    }
                });
            }

            lock (_stateLock)
            {
                return Task.FromResult(GetMetaInternal(saveId));
            }
        }

        private IDataContainer GetMetaInternal(string saveId)
        {
            Stream stream = null;
            try 
            {
                if (!TryGetStreamToRead(saveId, out stream) || stream.Length == 0) 
                {
                    if (!_useFallback || !TryGetFallbackStreamToRead(saveId, out stream))
                    {
                        return null;
                    }
                }
                
                if (!ValidatePrepareAndGetFallbackIfNeeded(ref stream, saveId, out int saveVersion))
                    return null;
                
                using Stream payloadStream = CreatePayloadStream(stream, saveVersion);
                var codec = GetCodec(saveVersion);
                if (codec == null)
                    return null;
                
                var values = DictionaryPool<string, ILoadStream>.Get();
                foreach (var (key, val) in codec.DecodeMeta(payloadStream))
                {
                    if (values.ContainsKey(key))
                    {
                        Debug.LogWarning($"Duplicate key found in save file meta: {key}. Skipping duplicate entry.");
                        continue;
                    }
                    values.Add(key, val);
                }
                return new SerializedDataContainer(values, this);
            }
            finally
            {
                if (stream != null)
                {
                    ReleaseStream(stream);
                }
            }
        }

        private bool ValidatePrepareAndGetFallbackIfNeeded(ref Stream s, string saveId, out int saveVersion) {
            
            if (!ValidateAndPrepareSave(s, out saveVersion)) {
                Debug.LogWarning("Save is corrupted.");
                ReleaseStream(s);
                if (!_useFallback)
                {
                    return false;
                }
                Debug.LogWarning("Attempt to load fallback save.");
                if (!TryGetFallbackStreamToRead(saveId, out s))
                {
                    Debug.LogWarning("Fail to load fallback save.");
                    return false;
                }
                if (!ValidateAndPrepareSave(s, out saveVersion))
                {
                    Debug.LogError("Fallback save is corrupted.");
                    return false;
                }
            }
            return true;
        }
        
        protected abstract bool TryGetStreamToRead(string saveId, out Stream stream);

        protected abstract bool TryGetFallbackStreamToRead(string saveId, out Stream stream);

        public abstract string[] GetSaves();

        public abstract void RemoveSave(string saveId);

        private unsafe IMutable<T> GetOrCreateMutableValue<T>(
            string name,
            T val = default,
            bool isMeta = false) where T : unmanaged
        {

            if (_createdMutableValues.TryGetValue(name, out var createdMutableValue))
            {
                if (createdMutableValue is IMutable<T> mutableVal)
                {
                    return mutableVal;
                }
                else
                {
                    Debug.LogError($"Created mutable value for key {name} is not of type IMutable<T>. This indicates a bug in the save system usage.");
                }
            }

            var capturedDataDict = isMeta ? _capturedMetaData : _capturedData;
            var capturedData = GetOrCreateCaptured<T>(capturedDataDict, () => new SaveData
            {
                Data = StaticBufferPool.Get(1, sizeof(T)),
                DataNames = new List<string>(),
            });
            
            if (capturedData.DataNames.Contains(name))
            {
                Debug.LogError($"Attempted to register duplicate save key: {name}. This indicates a bug in the save system usage. Returning existing value.");
                var list = (NativeList)capturedData.Data;
                var existingIndex = capturedData.DataNames.IndexOf(name);
                var existingVal = _mutableFactory.Create(name, val);
                existingVal.Bind((T x) => list.Set(existingIndex, &x));
                _createdMutableValues[name] = existingVal;
                return existingVal;
            }
            
            capturedData.DataNames.Add(name);
            var dataList = (NativeList)capturedData.Data;
            dataList.Add(&val);
            var index = dataList.Count - 1;
            var newVal = _mutableFactory.Create(name, val);
            newVal.Bind((T x) => dataList.Set(index, &x));

            _createdMutableValues.Add(name, newVal);
            return newVal;
        }

        private IMutable<T> CreateMutableCustomValue<T>(
            string name,
            T val = default,
            bool isMeta = false)
        {
            var capturedDataDict = isMeta ? _capturedMetaData : _capturedData;
            var capturedData = GetOrCreateCaptured<T>(capturedDataDict, () => new SaveData
            {
                Data = new List<T>(),
                DataNames = new List<string>(),
                CustomCodecProvider = ValidataAndGetCustomCodec<T>,
            });
            
            if (capturedData.DataNames.Contains(name))
            {
                if (_createdMutableValues.TryGetValue(name, out var duplicateCustomObj) &&
                    duplicateCustomObj is IMutable<T> duplicateCustomMutable)
                {
                    return duplicateCustomMutable;
                }
                Debug.LogError($"Attempted to register duplicate save key: {name}. This indicates a bug in the save system usage. Returning existing value.");
                var list = (List<T>)capturedData.Data;
                var existingIndex = capturedData.DataNames.IndexOf(name);
                var existingVal = _mutableFactory.Create(name, val);
                existingVal.Bind(x => list[existingIndex] = x);
                _createdMutableValues[name] = existingVal;
                return existingVal;
            }
            
            capturedData.DataNames.Add(name);
            var dataList = (List<T>)capturedData.Data;
            dataList.Add(val);
            var index = dataList.Count - 1;
            var newVal = _mutableFactory.Create(name, val);
            newVal.Bind(x => dataList[index] = x);
            _createdMutableValues.Add(name, newVal);
            return newVal;
        }

        private ICustomCodec ValidataAndGetCustomCodec<T>()
        {
            var codec = GetCustomCodec<T>();
            if (codec == null)
            {
                Debug.LogError($"Custom codec for type {typeof(T)} is not registered. Returning null.");
                return null;
            }
            return codec;
        }

        private T RegisterSavableValue<T>(string name, T val = default, bool isMeta = false)
            where T : ISaveObject
        {
            var capturedDataDict = isMeta ? _capturedMetaData : _capturedData;
            var capturedData = GetOrCreateCaptured<T>(capturedDataDict, () => new SaveData()
            {
                Data = new List<ISaveObject>(),
                DataNames = new List<string>(),
            });
            
            if (capturedData.DataNames.Contains(name))
            {
                Debug.LogError($"Attempted to register duplicate save key: {name}. This indicates a bug in the save system usage. Returning existing value.");
                var list = (List<ISaveObject>)capturedData.Data;
                var existingIndex = capturedData.DataNames.IndexOf(name);
                return (T)list[existingIndex];
            }
            
            val.Changed += OnAnyValueChanged;
            capturedData.DataNames.Add(name);
            var dataList = (List<ISaveObject>)capturedData.Data;
            dataList.Add(val);
            return val;
        }

        private static unsafe void WriteVersion(Stream s, int val)
        {
            var intBuffer = new Span<byte>((byte*)&val, sizeof(int));
            s.Write(intBuffer);
        }
        
        private static unsafe int ReadVersion(Stream s)
        {
            const int bytesLenght = sizeof(int);
            var intPtr = stackalloc byte[bytesLenght];
            var span = new Span<byte>(intPtr, bytesLenght);
            var count = s.Read(span);
            if (count != bytesLenght)
            {
                Debug.LogWarning("Fail to read version from stream. Unexpected end of file.");
                return -1;
            }
            return *(int*)intPtr;
        }

        private static SaveData GetOrCreateCaptured<T>(
            Dictionary<Type, SaveData> capturedDataDict, 
            Func<SaveData> defaultValueFactory)
        {
            if (!capturedDataDict.TryGetValue(typeof(T), out var capturedData))
            {
                capturedData = defaultValueFactory();
                capturedDataDict.Add(typeof(T), capturedData);
            }
            return capturedData;
        }

        public ICustomCodec GetCustomCodec(Type type) => _customCodecs[type];

        public ICustomCodec<T> GetCustomCodec<T>() => _customCodecs.TryGetValue(typeof(T), out var codec) ? (ICustomCodec<T>)codec : null;

        public void UseExplicitSaveSystemVersion(int version)
        {
            if (version < 0 || version >= _codecs.Count)
            {
                Debug.LogError($"Invalid save system version: {version}. Valid range is 0 to {_codecs.Count - 1}.");
                return;
            }
            _saveSystemVersion = version;
        }
    }
}