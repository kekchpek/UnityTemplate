using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionaryCU<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TValue : unmanaged
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadCustom<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadStruct<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key)
        {
            saveStream.SaveCustom(key);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value)
        {
            saveStream.SaveStruct(value);
        }
    }
}