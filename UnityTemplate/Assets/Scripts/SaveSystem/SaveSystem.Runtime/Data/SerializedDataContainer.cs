using System;
using System.Collections.Generic;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace kekchpek.SaveSystem.Data
{
    public class SerializedDataContainer : IDataContainer
    {

        private readonly Dictionary<string, ILoadStream> _values;
        private readonly ICustomCodecsProvider _customCodecsProvider;

        public SerializedDataContainer(Dictionary<string, ILoadStream> values, ICustomCodecsProvider customCodecsProvider)
        {
            _values = values;
            _customCodecsProvider = customCodecsProvider;
        }
        
        public unsafe T DeserializeStructValue<T>(string valueKey, bool removeAfterDeserialization, T defaultValue = default) where T : unmanaged
        {
            if (_values.TryGetValue(valueKey, out var value))
            {
                if (value == null)
                {
                    Debug.LogError($"The buffer for value with key {valueKey} is null.");
                    return defaultValue;
                }
                if (value.Data == null)
                {
                    Debug.LogError($"The Data property for value with key {valueKey} is null.");
                    return defaultValue;
                }
                if (value.Data.Data == null)
                {
                    Debug.LogError($"The Data.Data property for value with key {valueKey} is null.");
                    return defaultValue;
                }
                var val = *(T*)value.Data.Data;
                if (removeAfterDeserialization)
                {
                    value.Dispose();
                    _values.Remove(valueKey);
                }
                return val;
            }
            
            return defaultValue;
        }

        public T DeserializeSavableObject<T>(string valueKey, bool removeAfterDeserialization, Func<T> factoryMethod = null) where T : ISaveObject, new()
        {
            if (_values.TryGetValue(valueKey, out var value))
            {
                if (value == null)
                {
                    Debug.LogError($"The buffer for value with key {valueKey} is null.");
                    return factoryMethod == null ? new T() : factoryMethod();
                }
                var obj = value.LoadSavable<T>();
                if (removeAfterDeserialization)
                {
                    value.Dispose();
                    _values.Remove(valueKey);
                }
                return obj;
            }
            
            return factoryMethod == null ? new T() : factoryMethod();
        }

        public T DeserializeCustomValue<T>(string valueKey, bool removeAfterDeserialization, Func<T> defaultValueFactory = null)
        {
            var customCodec = _customCodecsProvider.GetCustomCodec<T>();
            if (customCodec == null)
            {
                Debug.LogError($"Custom codec for type {typeof(T)} is not registered. Returning default.");
                return defaultValueFactory == null ? default : defaultValueFactory();
            }
            if (_values.TryGetValue(valueKey, out var value))
            {
                if (value == null)
                {
                    // Null is expected for custom values. For example, string can be null.
                    if (default(T) == null)
                    {
                        if (removeAfterDeserialization)
                        {
                            _values.Remove(valueKey);
                        }
                        return default;
                    }
                    else 
                    {
                        Debug.LogError($"The buffer for value with key {valueKey} is null, but it is the struct value.");
                        return defaultValueFactory == null ? default : defaultValueFactory();
                    }
                }
                var obj = customCodec.Deserialize(value);
                if (removeAfterDeserialization)
                {
                    value.Dispose();
                    _values.Remove(valueKey);
                }
                return obj;
            }
            else
            {
                return defaultValueFactory == null ? default : defaultValueFactory();
            }
        }

        IEnumerable<(string key, NativeList data)> IDataContainer.GetNativeData()
        {
            foreach (var val in _values)
            {
                yield return (val.Key, val.Value.Data);
            }
        }

        public void Dispose()
        {
            foreach (var val in _values)
            {
                val.Value.Dispose();
            }
        }
    }
}