using System;
using System.Collections.Generic;
using System.IO;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;

namespace kekchpek.SaveSystem.Codec
{
    internal interface ISaveFileCodec
    {

        /// <summary>
        /// Decodes the binary format and read the data to the list of key-value pairs.
        /// </summary>
        /// <param name="inputStream">Input stream to read encoded data.</param>
        /// <returns></returns>
        IEnumerable<(string key, ILoadStream val, bool isMeta)> Decode(Stream inputStream);
        
        /// <summary>
        /// Decodes only metadata part of input stream.
        /// </summary>
        /// <param name="inputStream">Input stream to read encoded data.</param>
        /// <returns></returns>
        IEnumerable<(string key, ILoadStream val)> DecodeMeta(Stream inputStream);
        
        
        /// <summary>
        /// Encodes key-value pairs to the binary format and write it to the stream.
        /// </summary>
        /// <param name="outputStream">Output stream to write encoded data.</param>
        /// <param name="metaData">The collection of key-value pairs specific data types, that could be saved. This data
        /// will be saved to be able to be read without reading entire data.</param>
        /// <param name="data">The collection of key-value pairs specific data types, that could be saved.</param>
        void Encode(Stream outputStream, 
            IEnumerable<SaveData> metaData, IDataContainer untouchedMetaData,
            IEnumerable<SaveData> data, IDataContainer untouchedData);
    }
}