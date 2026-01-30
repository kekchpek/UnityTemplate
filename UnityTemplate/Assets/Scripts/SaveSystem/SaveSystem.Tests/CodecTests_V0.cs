using NUnit.Framework;
using kekchpek.SaveSystem.Codec;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using kekchpek.SaveSystem.CustomSerialization;
using kekchpek.SaveSystem.Data;

namespace kekchpek.SaveSystem.Tests
{

    [TestFixture]
    public class CodecTests_V0
    {

        public class CodecProvider : ICustomCodecsProvider
        {

            private readonly Dictionary<Type, ICustomCodec> _customCodecs = new() {
                { typeof(string), new StringCodec() }
            };

            public ICustomCodec GetCustomCodec(Type type) => _customCodecs[type];
            public ICustomCodec<T> GetCustomCodec<T>() => (ICustomCodec<T>)_customCodecs[typeof(T)];
        }
        
        [Test]
        public unsafe void TestEncodeDecodeString()
        {
            var codecProvider = new CodecProvider();
            var codec = new SaveFileCodecV0(codecProvider);
            var data = new SaveData
            {
                Data = new List<string> { "test" },
                DataNames = new List<string> { "testKey" },
                CustomCodecProvider = codecProvider.GetCustomCodec<string>
            };
            var buffer = new byte[1024];
            var stream = new MemoryStream(buffer);
            codec.Encode(stream, Array.Empty<SaveData>(), new SerializedDataContainer(new Dictionary<string, ILoadStream>(), codecProvider), new[] { data }, new SerializedDataContainer(new Dictionary<string, ILoadStream>(), codecProvider));
            var readStream = new MemoryStream(stream.ToArray());
            var decoded = codec.Decode(readStream);
            var (key, val, isMeta) = decoded.First();
            Assert.IsFalse(isMeta);
            var loadStream = LoadStream.Get(
                new UnmanagedMemoryStream((byte*)val.Data.Data, val.Data.AmountOfBytes, val.Data.AmountOfBytes, FileAccess.Read),
                null,
                codec);
            Assert.AreEqual(((List<string>)data.Data)[0], codecProvider.GetCustomCodec<string>().Deserialize(loadStream));
            Assert.AreEqual(data.DataNames[0], key);
        }

    }
}