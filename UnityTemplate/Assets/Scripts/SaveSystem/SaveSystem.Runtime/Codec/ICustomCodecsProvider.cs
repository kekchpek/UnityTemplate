using System;

namespace SaveSystem.Codec
{
    public interface ICustomCodecsProvider
    {
        ICustomCodec GetCustomCodec(Type type);

        ICustomCodec<T> GetCustomCodec<T>();

    }
}