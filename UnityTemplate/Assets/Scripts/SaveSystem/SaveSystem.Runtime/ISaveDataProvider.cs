using AsyncReactAwait.Bindable;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem
{
    public interface ISaveDataProvider
    {
        
        IMutable<T> DeserializeAndCaptureStructValue<T>(string valueKey, T defaultValue = default, bool isMetaValue = false) where T : unmanaged;

        T DeserializeAndCaptureSavableObject<T>(string valueKey, System.Func<T> factoryMethod = null, bool isMetaValue = false) where T : ISaveObject, new();

        IMutable<T> DeserializeAndCaptureCustomValue<T>(
            string valueKey, 
            System.Func<T> defaultValueFactory = null, 
            bool isMetaValue = false);
        
    }
}