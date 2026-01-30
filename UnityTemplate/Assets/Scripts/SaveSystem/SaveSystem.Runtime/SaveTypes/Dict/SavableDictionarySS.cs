using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionarySS<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TKey : ISaveObject, new()
        where TValue : ISaveObject, new()
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key)
        {
            key.Serialize(saveStream);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value)
        {
            value.Serialize(saveStream);
        }
    }
}