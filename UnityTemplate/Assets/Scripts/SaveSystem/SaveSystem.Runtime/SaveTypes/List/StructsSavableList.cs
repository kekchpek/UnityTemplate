using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.SaveTypes
{
    public class StructsSavableList<T> : BaseSavableList<T>
        where T : unmanaged
    {
        protected override T DeserializeInternal(ILoadStream loadStream)
        {
            return loadStream.LoadStruct<T>();
        }

        protected override void SerializeInternal(ISaveStream saveStream, T element)
        {
            saveStream.SaveStruct(element);
        }
    }
}