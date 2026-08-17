
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class CustomSavableList<T> : BaseSavableList<T>
    {
        protected override T DeserializeInternal(ILoadStream loadStream, int? customCodecVersion)
        {
            return loadStream.LoadCustom<T>();
        }

        protected override void SerializeInternal(ISaveStream saveStream, T element, int? customCodecVersion)
        {
            saveStream.SaveCustom(element, customCodecVersion);
        }
    }
}