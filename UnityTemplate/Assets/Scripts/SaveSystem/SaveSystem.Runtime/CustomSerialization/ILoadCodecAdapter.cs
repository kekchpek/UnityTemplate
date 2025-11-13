using System.IO;

namespace SaveSystem.CustomSerialization
{
    internal interface ILoadCodecAdapter
    {
        T ReadStruct<T>(Stream s) where T : unmanaged;
        T ReadCustom<T>(Stream s);
    }
}