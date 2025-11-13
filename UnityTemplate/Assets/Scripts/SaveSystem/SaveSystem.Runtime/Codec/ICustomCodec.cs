using System.IO;
using SaveSystem.CustomSerialization;

namespace SaveSystem.Codec
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