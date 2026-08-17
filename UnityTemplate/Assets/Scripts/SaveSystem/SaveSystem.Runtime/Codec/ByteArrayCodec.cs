using System;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.Codec
{
    public class ByteArrayCodec : ICustomCodec<byte[]>
    {
        public byte[] Deserialize(ILoadStream stream, int? saveVersionForCustomCodecs)
        {
            var count = stream.LoadStruct<int>();
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            var bytes = new byte[count];
            for (var i = 0; i < count; i++)
            {
                bytes[i] = stream.LoadStruct<byte>();
            }

            return bytes;
        }

        public void Serialize(ISaveStream stream, object value, int? saveVersionForCustomCodecs)
        {
            var bytes = (byte[])value ?? Array.Empty<byte>();
            stream.SaveStruct(bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
            {
                stream.SaveStruct(bytes[i]);
            }
        }
    }
}
