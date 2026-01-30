using System.IO;

namespace kekchpek.SaveSystem.CustomSerialization
{
    internal interface ISaveCodecAdapter
    {
        void WriteStruct<T>(Stream s, T val) where T : unmanaged;
        void WriteCustom<T>(Stream s, T val);
    }
}