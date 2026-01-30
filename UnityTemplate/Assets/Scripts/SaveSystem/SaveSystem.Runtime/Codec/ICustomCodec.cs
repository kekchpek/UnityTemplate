using System.IO;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.Codec
{
    public interface ICustomCodec<T> : ICustomCodec
    {
        T Deserialize(ILoadStream stream);
    }

    public interface ICustomCodec
    {
        void Serialize(ISaveStream stream, object value);
    }
}