using System;
using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.GameSaves.Mock
{
    public class EmptySaveDataProvider : IMultifileSaveDataProvider
    {
        private readonly IMutableFactory _mutableFactory;

        public EmptySaveDataProvider(IMutableFactory mutableFactory = null)
        {
            _mutableFactory = mutableFactory ?? DefaultMutableFactory.Instance;
        }

        public void ReleaseFile(string fileName, bool saveBeforeRelease = true)
        {
            // no-op
        }

        public IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged
        {
            return _mutableFactory.Create(valueKey, defaultValue);
        }

        public T DeserializeAndCaptureSavableObject<T>(string valueKey, Func<T> factoryMethod = null, bool isMetaValue = false) where T : ISaveObject, new()
        {
            return factoryMethod();
        }

        public IMutable<T> DeserializeAndCaptureCustomValue<T>(string valueKey, Func<T> defaultValueFactory = null, bool isMetaValue = false)
        {
            var value = defaultValueFactory != null ? defaultValueFactory() : default;
            return _mutableFactory.Create(valueKey, value);
        }
    }
}
