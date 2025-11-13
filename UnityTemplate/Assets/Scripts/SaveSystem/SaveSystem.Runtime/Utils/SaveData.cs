using System.Collections.Generic;
using SaveSystem.Codec;

namespace SaveSystem
{
    internal sealed class SaveData
    {
        public object Data { get; set; }
        public List<string> DataNames { get; set; }
        public ICustomCodec CustomCodec { get; set; }
    }
}