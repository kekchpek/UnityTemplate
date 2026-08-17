using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableDictionaryCC<TKey, TValue> : BaseSavableDictionary<TKey, TValue>
    {
        protected override TKey DeserializeKeyInternal(ILoadStream loadStream)
        {
            return loadStream.LoadCustom<TKey>();
        }

        protected override TValue DeserializeValueInternal(ILoadStream loadStream)
        {
            return loadStream.LoadCustom<TValue>();
        }

        protected override void SerializeKeyInternal(ISaveStream saveStream, TKey key, int? customCodecVersion)
        {
            saveStream.SaveCustom(key, customCodecVersion);
        }

        protected override void SerializeValueInternal(ISaveStream saveStream, TValue value, int? customCodecVersion)
        {
            saveStream.SaveCustom(value, customCodecVersion);
        }
    }
}