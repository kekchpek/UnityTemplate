using System.Collections.Generic;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.GameSaves.Codecs
{
    public class StringHashSetCodec : ICustomCodec<HashSet<string>>
    {
        public HashSet<string> Deserialize(ILoadStream stream, int? customCodecVersion)
        {
            var set = new HashSet<string>();
            var count = stream.LoadStruct<int>();

            for (int i = 0; i < count; i++)
            {
                set.Add(stream.LoadCustom<string>());
            }

            return set;
        }

        public void Serialize(ISaveStream stream, object value, int? customCodecVersion)
        {
            if (value == null)
            {
                stream.SaveStruct(0);
                return;
            }

            var set = (HashSet<string>)value;
            stream.SaveStruct(set.Count);

            foreach (var str in set)
            {
                stream.SaveCustom(str ?? string.Empty, customCodecVersion);
            }
        }
    }
}
