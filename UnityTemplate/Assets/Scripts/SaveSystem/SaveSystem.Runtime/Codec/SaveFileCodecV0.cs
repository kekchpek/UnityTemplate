using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Utils;
using UnityEngine;
using kekchpek.SaveSystem.Data;

namespace kekchpek.SaveSystem.Codec
{
    internal class SaveFileCodecV0 : ISaveFileCodec, ISaveCodecAdapter, ILoadCodecAdapter
    {

        private readonly ICustomCodecsProvider _customCodecsProvider;

        public SaveFileCodecV0(ICustomCodecsProvider customCodecsProvider)
        {
            _customCodecsProvider = customCodecsProvider;
        }

        public IEnumerable<(string key, ILoadStream val, bool isMeta)> Decode(Stream inputStream)
        {
            var metaDataLength = ReadStruct<short>(inputStream);
            var initialPosition = inputStream.Position;
            foreach (var (key, val) in DecodeInternal(inputStream))
            {
                yield return (key, val, inputStream.Position - initialPosition <= metaDataLength);
            }
        }

        public IEnumerable<(string key, ILoadStream val)> DecodeMeta(Stream inputStream)
        {
            var metaDataLength = ReadStruct<short>(inputStream);
            var metaBytes = new byte[metaDataLength];
            var readBytes = inputStream.Read(metaBytes);
            if (readBytes != metaDataLength)
            {
                Debug.LogError($"Fail to read metadata from stream. Unexpected end of stream. " + 
                    $"Expected length {metaDataLength}. Read bytes {readBytes}");
                yield break;
            }
            var metaStream = new MemoryStream(metaBytes);
            foreach (var valueTuple in DecodeInternal(metaStream))
            {
                yield return valueTuple;
            }
        }

        private IEnumerable<(string key, ILoadStream val)> DecodeInternal(Stream inputStream)
        {
            while (true)
            {
                if (TryReadKey(inputStream, out var key))
                {
                    var value = ReadValue(inputStream);
                    if (!string.IsNullOrEmpty(key))
                    {
                        yield return (key, value);
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping empty or null key in save file");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void Encode(Stream outputStream, 
            IEnumerable<SaveData> metaData, IDataContainer untouchedMetaData,
            IEnumerable<SaveData> data, IDataContainer untouchedData)
        {
            using (var stream = new MemoryStream())
            {
                EncodeInternal(stream, metaData);
                EncodeUntouchedData(stream, untouchedMetaData);
                var metaBytes = stream.ToArray();
                WriteStruct(outputStream, (short)metaBytes.Length);
                outputStream.Write(metaBytes);
            }


            EncodeInternal(outputStream, data);
            EncodeUntouchedData(outputStream, untouchedData);
        }

        private unsafe void EncodeUntouchedData(Stream outputStream, IDataContainer dataContainer)
        {
            if (dataContainer == null)
            {
                Debug.LogWarning("Data container is null when encoding untouched data");
                return;
            }

            foreach (var (key, nativeList) in dataContainer.GetNativeData())
            {
                if (nativeList == null || nativeList.Data == null)
                {
                    Debug.LogWarning($"Skipping null native list for key {key}");
                    continue;
                }

                try
                {
                    WriteString(outputStream, key);
                    var size = nativeList.Count * nativeList.ElementSize;
                    WriteStruct(outputStream, (short)size);
                    var dataSpan = new Span<byte>(nativeList.Data, size);
                    outputStream.Write(dataSpan);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error encoding untouched data for key {key}: {e.Message}");
                }
            }
        }

        private unsafe void EncodeInternal(Stream outputStream, IEnumerable<SaveData> data)
        {
            foreach (var saveData in data)
            {
                var customCodec = saveData.CustomCodecProvider?.Invoke();
                if (customCodec == null) {
                    var value = saveData.Data;
                    switch (value)
                    {
                        case List<ISaveObject> saveObjects:
                            for (var i = 0; i < saveObjects.Count; i++)
                            {
                                WriteString(outputStream, saveData.DataNames[i]);
                                var memoryStream = new MemoryStream();
                                var saveObject = saveObjects[i];
                                var saveStream = SaveStream.Get(memoryStream, this);
                                saveObject.Serialize(saveStream);
                                saveStream.Dispose();
                                var bytes = memoryStream.ToArray();
                                WriteStruct(outputStream, (short)bytes.Length);
                                outputStream.Write(bytes);
                            }

                            break;
                        case NativeList nativeList:
                            for (var i = 0; i < nativeList.Count; i++)
                            {
                                try {
                                WriteString(outputStream, saveData.DataNames[i]);
                                WriteStruct(outputStream, (short)nativeList.ElementSize);
                                var dataSpan = new Span<byte>(nativeList.Get(i), nativeList.ElementSize);
                                outputStream.Write(dataSpan);
                                }
                                catch (Exception e) {
                                    Debug.LogError($"Fail to write native list to stream. {e.Message}");
                                }
                            }
                            break;
                        default:
                            Debug.LogError($"Save data of unknown type {value.GetType().Name}.");
                            break;
                    }
                }
                else 
                {
                    if (saveData.Data is IEnumerable enumerable)
                    {
                        var i = 0;
                        foreach (var o in enumerable)
                        {   
                            WriteString(outputStream, saveData.DataNames[i++]);
                            WriteCustom(outputStream, o, customCodec);
                        }
                    }
                    else 
                    {
                        Debug.LogError($"Save data of type {saveData.Data.GetType().Name} should be enumerable.");
                    }
                }
            }
        }

        public void WriteCustom<T>(Stream s, T val)
        {
            WriteCustom(s, val, _customCodecsProvider.GetCustomCodec<T>());
        }

        private void WriteCustom(Stream s, object val, ICustomCodec customCodec)
        {
            using MemoryStream tmpStream = new MemoryStream();
            using var saveStream = SaveStream.Get(tmpStream, this);
            customCodec.Serialize(saveStream, val);
            var bytes = tmpStream.ToArray();
            WriteStruct(s, (short)bytes.Length);
            s.Write(bytes);
        }

        public unsafe void WriteStruct<T>(Stream s, T val)
            where T : unmanaged
        {
            var intBuffer = new Span<byte>((byte*)&val, sizeof(T));
            s.Write(intBuffer);
        }

        private unsafe void WriteString(Stream s, string val)
        {
            fixed (char* stringPtr = val.AsSpan())
            {
                var stringBuffer = new Span<byte>((byte*)stringPtr, val.Length * sizeof(char));
                if (val.Length > byte.MaxValue)
                {
                    Debug.LogError($"String length {val.Length} is greater than {byte.MaxValue}.");
                    return;
                }
                WriteStruct(s, (short)val.Length);
                s.Write(stringBuffer);
            }
        }

        public unsafe T ReadStruct<T>(Stream s) where T : unmanaged
        {
            var bufferPtr = stackalloc byte[sizeof(T)];
            var span = new Span<byte>(bufferPtr, sizeof(T));
            var count = s.Read(span);
            if (count == 0)
            {
                throw new EndOfStreamException("No data to read.");
            }
            if (count != sizeof(T))
                throw new Exception("Fail to read int from stream. Unexpected end of stream. " +
                    $"Expected {sizeof(T)} bytes, read {count} bytes.");
            return *(T*)bufferPtr;
        }

        public T ReadCustom<T>(Stream s)
        {
            var bytesLength = ReadStruct<short>(s);
            var bytes = new byte[bytesLength];  
            var readBytes = s.Read(bytes);
            if (readBytes != bytesLength)
            {
                Debug.LogError($"Fail to read custom value from stream. Unexpected end of stream. " + 
                    $"Expected length {bytesLength}. Read bytes {readBytes}");
                return default;
            }
            using var stream = LoadStream.Get(new MemoryStream(bytes), null, this);
            return _customCodecsProvider.GetCustomCodec<T>().Deserialize(stream);
        }

        private unsafe ILoadStream ReadValue(Stream s)
        {
            var bytesLength = ReadStruct<short>(s);
            if (bytesLength == 0)
            {
                return null;
            }
            // Acquire unmanaged buffer that will hold the raw bytes.
            var buffer = StaticBufferPool.Get(bytesLength, 1);

            // Fill the buffer with data from the input stream.
            var destinationSpan = new Span<byte>(buffer.Data, bytesLength);
            var readBytes = s.Read(destinationSpan);
            if (readBytes != bytesLength)
                throw new Exception("Fail to read custom value from stream. Unexpected end of stream. " + 
                    $"Expected length {bytesLength}. Read bytes {readBytes}");
            buffer.SetCount(readBytes);

            // Wrap the unmanaged buffer into a stream for further structured reads.
            var unmanagedStream = new UnmanagedMemoryStream((byte*)buffer.Data, bytesLength, bytesLength, FileAccess.Read);
            return LoadStream.Get(unmanagedStream, buffer, this);
        }

        private unsafe bool TryReadKey(Stream s, out string key)
        {
            int valueLength;

            try {
                valueLength = ReadStruct<short>(s);
            }
            catch (EndOfStreamException)
            {
                key = null;
                return false;
            }
            var bytesLenght = valueLength * sizeof(char);
            var stringBufferPtr = stackalloc byte[bytesLenght];
            var span = new Span<byte>(stringBufferPtr, bytesLenght);
            var count = s.Read(span);
            if (count != bytesLenght)
            {
                throw new Exception($"Fail to read string from stream. Unexpected end of stream. " + 
                $"Expected {bytesLenght} bytes, read {count} bytes.");
            }
            key = new string((char*)stringBufferPtr, 0, valueLength);
            return true;
        }
    }
}