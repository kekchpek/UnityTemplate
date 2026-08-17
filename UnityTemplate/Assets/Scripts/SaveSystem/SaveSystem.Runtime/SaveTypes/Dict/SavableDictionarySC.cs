using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionarySC<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
        where TKey : ISaveObject, new()
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadCustom<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key, int? customCodecVersion)
        {
            key.Serialize(saveStream, customCodecVersion);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value, int? customCodecVersion)
        {
            saveStream.SaveCustom(value, customCodecVersion);
        }
    }
}