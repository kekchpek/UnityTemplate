using System;
using System.IO;

namespace SaveSystem.CustomSerialization
{
    public interface ISaveStream : IDisposable
    {
        
        internal Stream Stream { get; }

        void SaveStruct<T>(T val) where T : unmanaged;
        void SaveSabable<T>(T val) where T : ISaveObject, new();
        void SaveCustom<T>(T val);
    }
}