using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;

namespace kekchpek.GameSaves.Data
{
    public abstract class BaseSaveData
    {

        private IDataContainer _dataContainer;

        internal BaseSaveData()
        {
        }

        internal void SetDataContainer(IDataContainer dataContainer)
        {
            _dataContainer = dataContainer;
        }

        
        protected T GetStructValue<T>(string key) where T : unmanaged
        {
            return _dataContainer.DeserializeStructValue<T>(key, false);
        }

        protected T GetCustomValue<T>(string key)
        {
            return _dataContainer.DeserializeCustomValue<T>(key, false);
        }

        protected T GetSavableValue<T>(string key) where T : ISaveObject, new()
        {
            return _dataContainer.DeserializeSavableObject<T>(key, false);
        }
    }
}