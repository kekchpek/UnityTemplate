using System;
using System.Collections.Generic;
using kekchpek.SaveSystem.Codec;

namespace kekchpek.SaveSystem
{
    internal sealed class SaveData
    {
        public object Data { get; set; }
        public List<string> DataNames { get; set; }
        public Func<ICustomCodec> CustomCodecProvider { get; set; }
    }
}