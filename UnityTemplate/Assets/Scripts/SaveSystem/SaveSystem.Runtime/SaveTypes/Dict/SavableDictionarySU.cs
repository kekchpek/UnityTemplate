using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionarySU<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TKey : ISaveObject, new()
        where TValue : unmanaged
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadStruct<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key, int? customCodecVersion)
        {
            key.Serialize(saveStream, customCodecVersion);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value, int? customCodecVersion)
        {
            saveStream.SaveStruct(value);
        }
    }
}