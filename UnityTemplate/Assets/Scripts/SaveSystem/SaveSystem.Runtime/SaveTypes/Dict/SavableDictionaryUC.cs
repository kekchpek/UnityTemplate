using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionaryUC<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TKey : unmanaged
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadStruct<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadCustom<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key)
        {
            saveStream.SaveStruct(key);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value)
        {
            saveStream.SaveCustom(value);
        }
    }
}