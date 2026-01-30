using System.Collections.Generic;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.GameSaves.Codecs
{
    public class StringArrayCodec : ICustomCodec<string[]>
    {
        public string[] Deserialize(ILoadStream stream)
        {
            var count = stream.LoadStruct<int>();
            var stringArray = new string[count];
            for (int i = 0; i < count; i++)
            {
                stringArray[i] = stream.LoadCustom<string>();
            }
            
            return stringArray;
        }

        public void Serialize(ISaveStream stream, object value)
        {
            if (value == null) 
            {
                stream.SaveStruct(0);
                return;
            }
            
            var stringArray = (string[])value;
            stream.SaveStruct(stringArray.Length);
            foreach (var str in stringArray)
            {
                stream.SaveCustom(str ?? string.Empty);
            }
        }
    }
}
