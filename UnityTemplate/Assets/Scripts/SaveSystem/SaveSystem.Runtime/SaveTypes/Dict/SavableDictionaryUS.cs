using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionaryUS<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TKey : unmanaged
        where TValue : ISaveObject, new()
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadStruct<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key)
        {
            saveStream.SaveStruct(key);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value)
        {
            value.Serialize(saveStream);
        }
    }
}