using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class StructsSavableList<T> : BaseSavableList<T>
        where T : unmanaged
    {
        protected override T DeserializeInternal(ILoadStream loadStream, int? customCodecVersion)
        {
            return loadStream.LoadStruct<T>();
        }

        protected override void SerializeInternal(ISaveStream saveStream, T element, int? customCodecVersion)
        {
            saveStream.SaveStruct(element);
        }
    }
}