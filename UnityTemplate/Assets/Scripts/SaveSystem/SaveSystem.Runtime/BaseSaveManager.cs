using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public abstract class BaseSaveManager : 
        ISaveManager, 
        ICustomCodecsProvider, 
        ICustomCodecsRegister, 
        ISaveDataProvider
    {


        public int SaveOnChangesDebounceMs { get; set; } = 5000;
        public int MaxSaveOnChangesTimeMs { get; set; } = 20000;
        public string CurrentSaveId { get; private set; }
        public bool SaveOnChangesEnabled { get; set; }

        private readonly IReadOnlyList<ISaveFileCodec> _codecs;
        
        private readonly Dictionary<Type, SaveData> _capturedData = new();
        private readonly Dictionary<Type, SaveData> _capturedMetaData = new();

        private readonly Dictionary<Type, ICustomCodec> _customCodecs = new();
        
        private IDataContainer _loadedDataContainer;
        private IDataContainer _loadedMetaDataContainer;

        private readonly object _stateLock = new();
        private bool _loading;
        private bool _saving;
        private long _lastSaveTime;
        
        private CancellationTokenSource _debounceSaveCts;

        protected BaseSaveManager()
        {
            _codecs = new ISaveFileCodec[] {
                new SaveFileCodecV0(this),
            };
            RegisterCustomCodec(new StringCodec());
        }

        public IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged
        {
            lock (_stateLock)
            {
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
                var newVal = CreateMutableValue(valueKey, val, isMetaValue);
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
                var dataContainer = isMetaValue ? _loadedMetaDataContainer : _loadedDataContainer;
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
                var dataContainer = isMetaValue ? _loadedMetaDataContainer : _loadedDataContainer;
                var val = dataContainer.DeserializeSavableObject(valueKey, true, factoryMethod);
                return RegisterSavableValue(valueKey, val, isMetaValue);
            }
        }

        public void RegisterCustomCodec<T>(ICustomCodec<T> codec)
        {
            var type = typeof(T);
            if (_customCodecs.ContainsKey(type)) {
                return;
            }
            _customCodecs.Add(type, codec);
        }

        public void RemoveCustomCodec<T>() 
        {
            _customCodecs.Remove(typeof(T));
        }

        public void SaveExplicitly()
        {
            if (_debounceSaveCts != null)
            {
                Debug.Log("Cancel debounced save. Because of explicit saving.");
                _debounceSaveCts.Cancel();
                _debounceSaveCts.Dispose();
                _debounceSaveCts = null;
            }
            SaveInternal();
        }

        private void SaveInternal()
        {
            if (_saving)
            {
                return;
            }
            
            if (CurrentSaveId == null)
            {
                Debug.LogError("Attempt to save while no save id set.");
            }
            if (_loading)
            {
                Debug.LogError("Attempt to save while loading.");
                return;
            }
            if (_saving)
            {
                Debug.LogError("Attempt to save while saving.");
                return;
            }

            Stream stream = null;
            try
            {
                _saving = true;
                stream = GetStreamToWrite(CurrentSaveId);
                var codec = _codecs[^1];
                WriteVersion(stream, _codecs.Count - 1);
                codec.Encode(stream, 
                    _capturedMetaData.Values, _loadedMetaDataContainer,
                    _capturedData.Values, _loadedDataContainer);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving save {CurrentSaveId}: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                if (stream != null)
                {
                    ReleaseStream(stream);
                }
                _saving = false;
                _lastSaveTime = Stopwatch.GetTimestamp();
            }
        }

        protected abstract Stream GetStreamToWrite(string saveId);

        private async void DebounceSave()
        {
            _debounceSaveCts?.Cancel();
            _debounceSaveCts?.Dispose();
            _debounceSaveCts = new CancellationTokenSource();
            var currentTime = Stopwatch.GetTimestamp();
            var maxSaveOnChangesTimeTicks = MaxSaveOnChangesTimeMs * (long)10000;
            if (currentTime - _lastSaveTime > maxSaveOnChangesTimeTicks)
            {
                await Task.Run(SaveInternal).ConfigureAwait(false);
                return;
            }
            try
            {
                await Task.Delay((int)Math.Min(SaveOnChangesDebounceMs, _lastSaveTime + maxSaveOnChangesTimeTicks - currentTime), 
                    cancellationToken: _debounceSaveCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            _debounceSaveCts?.Dispose();
            _debounceSaveCts = null;
            
            await Task.Run(SaveInternal).ConfigureAwait(false);
        }

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
                _lastSaveTime = Stopwatch.GetTimestamp();
                foreach (var (_, val) in _capturedData)
                {
                    if (val.Data is NativeList nativeList)
                    {
                        StaticBufferPool.Release(nativeList);
                    }
                }
                _capturedData.Clear();
                foreach (var (_, val) in _capturedMetaData)
                {
                    if (val.Data is NativeList nativeList)
                    {
                        StaticBufferPool.Release(nativeList);
                    }
                }
                _capturedMetaData.Clear();
                if (!TryGetStreamToRead(saveId, out s) || s.Length == 0) // Empty stream means new save - create empty data container
                {
                    var emptyValues = DictionaryPool<string, ILoadStream>.Get();
                    _loadedDataContainer = new SerializedDataContainer(emptyValues, this);
                    _loadedMetaDataContainer = new SerializedDataContainer(emptyValues, this);
                    return;
                }
                
                var codec = GetCodec(s);
                if (codec == null)
                    return;
                
                var values = DictionaryPool<string, ILoadStream>.Get();
                var metaValues = DictionaryPool<string, ILoadStream>.Get();
                foreach (var (key, val, isMeta) in codec.Decode(s))
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

        protected abstract void ReleaseStream(Stream s);

        private ISaveFileCodec GetCodec(Stream s)
        {
            var v = ReadVersion(s);
            if (v < 0)
            {
                Debug.LogError("Fail to read version from stream.");
                return null;
            }
            if (v < _codecs.Count)
            {
                return _codecs[v];
            }
            Debug.LogError($"Can not load file with version {v}. No suitable codec.");
            return null;
        }

        public async Task<IDataContainer> GetMetaData(string saveId)
        {
            return await Task.Run(() =>
            {
                lock (_stateLock)
                {
                    return GetMetaInternal(saveId);
                }
            }).ConfigureAwait(false);
        }

        private IDataContainer GetMetaInternal(string saveId)
        {
            Stream stream = null;
            try 
            {
                if (!TryGetStreamToRead(saveId, out stream)) 
                    return null;
                
                var codec = GetCodec(stream);
                if (codec == null)
                    return null;
                
                var values = DictionaryPool<string, ILoadStream>.Get();
                foreach (var (key, val) in codec.DecodeMeta(stream))
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
        
        protected abstract bool TryGetStreamToRead(string saveId, out Stream stream);

        public abstract string[] GetSaves();

        private unsafe IMutable<T> CreateMutableValue<T>(string name, T val = default, bool isMeta = false) where T : unmanaged
        {
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
                var existingVal = new Mutable<T>(val);
                existingVal.Bind((T x) => list.Set(existingIndex, &x));
                existingVal.Bind(OnAnyValueChanged, false);
                return existingVal;
            }
            
            capturedData.DataNames.Add(name);
            var dataList = (NativeList)capturedData.Data;
            dataList.Add(&val);
            var index = dataList.Count - 1;
            var newVal = new Mutable<T>(val);
            newVal.Bind((T x) => dataList.Set(index, &x));
            newVal.Bind(OnAnyValueChanged, false);

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

        private IMutable<T> CreateMutableCustomValue<T>(string name, T val = default, bool isMeta = false)
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
                Debug.LogError($"Attempted to register duplicate save key: {name}. This indicates a bug in the save system usage. Returning existing value.");
                var list = (List<T>)capturedData.Data;
                var existingIndex = capturedData.DataNames.IndexOf(name);
                var existingVal = new Mutable<T>(val);
                existingVal.Bind(x => list[existingIndex] = x);
                existingVal.Bind(OnAnyValueChanged, false);
                return existingVal;
            }
            
            capturedData.DataNames.Add(name);
            var dataList = (List<T>)capturedData.Data;
            dataList.Add(val);
            var index = dataList.Count - 1;
            var newVal = new Mutable<T>(val);
            newVal.Bind(x => dataList[index] = x);
            newVal.Bind(OnAnyValueChanged, false);
            return newVal;
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

        private void OnAnyValueChanged()
        {
            if (SaveOnChangesEnabled)
                DebounceSave();
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
                Debug.LogError("Fail to read version from stream. Unexpected end of file.");
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
    }
}