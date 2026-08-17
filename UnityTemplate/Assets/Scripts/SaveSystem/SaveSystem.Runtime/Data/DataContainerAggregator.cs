using System;
using System.Collections.Generic;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Utils;
using UnityEngine;

namespace kekchpek.SaveSystem.Data
{
    public class DataContainerAggregator : IDataContainer
    {
        private const string DefaultContainerKey = "defaultData";

        private readonly Dictionary<string, IDataContainer> _dataContainers = new();

        public void AddContainer(string key, IDataContainer dataContainer)
        {
            _dataContainers[key] = dataContainer;
        }

        public T DeserializeCustomValue<T>(
            string valueKey,
            bool removeAfterDeserialization,
            Func<T> defaultValueFactory = null)
        {
            if (!TryResolveValueKey(valueKey, out var container, out var innerKey))
            {
                return defaultValueFactory == null ? default : defaultValueFactory();
            }

            return container.DeserializeCustomValue(innerKey, removeAfterDeserialization, defaultValueFactory);
        }

        public T DeserializeSavableObject<T>(
            string valueKey,
            bool removeAfterDeserialization,
            Func<T> factoryMethod = null) where T : ISaveObject, new()
        {
            if (!TryResolveValueKey(valueKey, out var container, out var innerKey))
            {
                return factoryMethod == null ? new T() : factoryMethod();
            }

            return container.DeserializeSavableObject(innerKey, removeAfterDeserialization, factoryMethod);
        }

        public T DeserializeStructValue<T>(
            string valueKey,
            bool removeAfterDeserialization,
            T defaultValue = default) where T : unmanaged
        {
            if (!TryResolveValueKey(valueKey, out var container, out var innerKey))
            {
                return defaultValue;
            }

            return container.DeserializeStructValue(innerKey, removeAfterDeserialization, defaultValue);
        }

        IEnumerable<(string key, NativeList data)> IDataContainer.GetNativeData()
        {
            foreach (var (containerKey, container) in _dataContainers)
            {
                foreach (var (key, data) in container.GetNativeData())
                {
                    var aggregatedKey = containerKey == DefaultContainerKey
                        ? key
                        : $"{containerKey}:{key}";
                    yield return (aggregatedKey, data);
                }
            }
        }

        private bool TryResolveValueKey(string valueKey, out IDataContainer container, out string innerKey)
        {
            var parts = valueKey.Split(':');
            string containerKey;
            if (parts.Length == 1)
            {
                containerKey = DefaultContainerKey;
                innerKey = valueKey;
            }
            else if (parts.Length == 2)
            {
                containerKey = parts[0];
                innerKey = parts[1];
            }
            else
            {
                Debug.LogError($"Only one ':' is allowed in the value key. Value key: {valueKey}");
                containerKey = DefaultContainerKey;
                innerKey = valueKey;
            }

            if (_dataContainers.TryGetValue(containerKey, out container))
            {
                return true;
            }

            Debug.LogError($"Data container '{containerKey}' is not registered.");
            container = null;
            innerKey = null;
            return false;
        }

        public void Dispose()
        {
            foreach (var container in _dataContainers.Values)
            {
                container.Dispose();
            }
            _dataContainers.Clear();
        }
    }
}
