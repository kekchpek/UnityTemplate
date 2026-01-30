using System;
using System.IO;
using kekchpek.SaveSystem.Utils;

namespace kekchpek.SaveSystem.CustomSerialization
{
    public interface ILoadStream : IDisposable
    {

        /// <summary>
        /// The memory buffer that contains data to load.
        /// </summary>
        internal NativeList Data { get; }

        /// <summary>
        /// The for handling data to load. Has <see cref="Data"/> under the hood.
        /// </summary>
        internal Stream Stream { get; }

        T LoadStruct<T>() where T : unmanaged;
        T LoadSavable<T>() where T : ISaveObject, new();
        T LoadCustom<T>();
        bool IsEnd();
    }
}