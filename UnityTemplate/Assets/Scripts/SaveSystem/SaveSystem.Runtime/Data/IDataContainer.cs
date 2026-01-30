using System;
using System.Collections.Generic;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Utils;

namespace kekchpek.SaveSystem.Data
{
    public interface IDataContainer : IDisposable
    {
        
        T DeserializeStructValue<T>(
            string valueKey, 
            bool removeAfterDeserialization,
            T defaultValue = default) where T : unmanaged;

        T DeserializeSavableObject<T>(
            string valueKey,
            bool removeAfterDeserialization,
            System.Func<T> factoryMethod = null) where T : ISaveObject, new();

        T DeserializeCustomValue<T>(
            string valueKey, 
            bool remvoeAfterDeserialization,
            System.Func<T> defaultValueFactory = null);

        internal IEnumerable<(string key, NativeList data)> GetNativeData();

    }
}