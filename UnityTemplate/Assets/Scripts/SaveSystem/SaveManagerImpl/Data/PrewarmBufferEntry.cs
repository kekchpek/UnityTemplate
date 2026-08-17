using Newtonsoft.Json;

namespace kekchpek.GameSaves.Data
{
    public sealed class PrewarmBufferEntry
    {
        [JsonProperty()]
        private int size;

        [JsonProperty()]
        private int elementSize;

        [JsonProperty()]
        private int count;

        public int Size => size;

        public int ElementSize => elementSize;

        public int Count => count;
    }
}
