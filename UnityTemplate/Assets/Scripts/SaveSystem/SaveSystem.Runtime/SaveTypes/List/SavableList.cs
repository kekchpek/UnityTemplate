using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class SavableList<T> : BaseSavableList<T>
        where T : ISaveObject, new()
    {
        protected override T DeserializeInternal(ILoadStream loadStream, int? customCodecVersion)
        {
            return loadStream.LoadSavable<T>();
        }

        protected override void SerializeInternal(ISaveStream saveStream, T element, int? customCodecVersion)
        {
            element.Serialize(saveStream, customCodecVersion);
        }
    }
}