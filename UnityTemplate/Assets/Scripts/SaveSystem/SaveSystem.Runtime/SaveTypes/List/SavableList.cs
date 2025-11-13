using SaveSystem.CustomSerialization;

namespace SaveSystem.SaveTypes
{
    public class SavableList<T> : BaseSavableList<T>
        where T : ISaveObject, new()
    {
        protected override T DeserializeInternal(ILoadStream loadStream)
        {
            return loadStream.LoadSavable<T>();
        }

        protected override void SerializeInternal(ISaveStream saveStream, T element)
        {
            element.Serialize(saveStream);
        }
    }
}