using System.Collections.Generic;
using kekchpek.Auxiliary.ReactiveList;
using kekchpek.SaveSystem.Codec;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.GameSaves.Codecs
{
    public class StringListCodec : ICustomCodec<MutableList<string>>
    {
        public MutableList<string> Deserialize(ILoadStream stream)
        {
            var stringList = new MutableList<string>();
            var count = stream.LoadStruct<int>();
            
            for (int i = 0; i < count; i++)
            {
                stringList.Add(stream.LoadCustom<string>());
            }
            
            return stringList;
        }

        public void Serialize(ISaveStream stream, object value)
        {
            if (value == null) 
            {
                stream.SaveStruct(0);
                return;
            }
            
            var stringList = (MutableList<string>)value;
            stream.SaveStruct(stringList.Count);
            
            foreach (var str in stringList)
            {
                stream.SaveCustom(str ?? string.Empty);
            }
        }
    }
}
