using System;
using System.IO;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.Codec
{
    public class StringCodec : ICustomCodec<string>
    {
        public string Deserialize(ILoadStream stream)
        {
            var b = stream.LoadStruct<bool>();
            if (!b)
            {
                return string.Empty;
            }
            else 
            {
                return new StreamReader(stream.Stream).ReadToEnd();
            }
        }

        public void Serialize(ISaveStream stream, object value)
        {
            if (value == null)
            {
                return;
            }
            var s = value.ToString();
            if (s.Length == 0)
            {
                stream.SaveStruct(false);
            }
            else
            {
                stream.SaveStruct(true);
                using StreamWriter writer = new StreamWriter(stream.Stream);
                writer.Write(value);
            }
        }
    }
}