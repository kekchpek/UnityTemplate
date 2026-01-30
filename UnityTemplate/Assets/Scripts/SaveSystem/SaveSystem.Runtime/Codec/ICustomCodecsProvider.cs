using System;

namespace kekchpek.SaveSystem.Codec
{
    public interface ICustomCodecsProvider
    {
        ICustomCodec GetCustomCodec(Type type);

        ICustomCodec<T> GetCustomCodec<T>();

    }
}