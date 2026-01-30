using System;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.GameSaves.Mock
{
    public class EmptySaveDataProvider : ISaveDataProvider
    {
        public IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged
        {
            return new Mutable<T>(defaultValue);
        }

        public T DeserializeAndCaptureSavableObject<T>(string valueKey, Func<T> factoryMethod = null, bool isMetaValue = false) where T : ISaveObject, new()
        {
            return factoryMethod();
        }

        public IMutable<T> DeserializeAndCaptureCustomValue<T>(string valueKey, Func<T> defaultValueFactory = null, bool isMetaValue = false)
        {
            return new Mutable<T>(defaultValueFactory());
        }
    }
}