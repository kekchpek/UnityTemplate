using System.IO;

namespace kekchpek.SaveSystem.CustomSerialization
{
    internal interface ILoadCodecAdapter
    {
        T ReadStruct<T>(Stream s) where T : unmanaged;
        T ReadCustom<T>(Stream s);
    }
}