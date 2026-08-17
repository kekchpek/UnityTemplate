using BigInteger = System.Numerics.BigInteger;
using kekchpek.SaveSystem.CustomSerialization;

namespace kekchpek.SaveSystem.Codec
{
    public class BigIntegerCodec : ICustomCodec<BigInteger>
    {
        public BigInteger Deserialize(ILoadStream stream, int? saveVersionForCustomCodecs)
        {
            var bytes = stream.LoadCustom<byte[]>();
            if (bytes == null || bytes.Length == 0)
            {
                return BigInteger.Zero;
            }

            return new BigInteger(bytes);
        }

        public void Serialize(ISaveStream stream, object value, int? saveVersionForCustomCodecs)
        {
            stream.SaveCustom(((BigInteger)value).ToByteArray(), saveVersionForCustomCodecs);
        }
    }
}
