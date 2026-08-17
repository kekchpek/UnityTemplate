using System;
using BigInteger = System.Numerics.BigInteger;

namespace kekchpek.MVVM.Models.GameResources.Static
{
    public static class ResourceFormatting
    {

        public static string FormatNumber(double value)
        {
            double absValue = Math.Abs(value);
            
            if (absValue < 1_000_000.0)
            {
                return ToString(value);
            }
            else if (absValue < 1_000_000_000.0)
            {
                double divided = value / 1_000.0;
                return ToString(divided) + "M";
            }
            else if (absValue < 1_000_000_000_000.0)
            {
                double divided = value / 1_000_000.0;
                return ToString(divided) + "B";
            }
            else
            {
                double divided = value / 1_000_000_000.0;
                return ToString(divided) + "T";
            }
        }

        public static string FormatNumber(float value)
        {
            return FormatNumber((double)value);
        }

        public static string FormatNumber(BigInteger value)
        {
            if (value < 1_000_000)
            {
                return ToString(value);
            }
            else if (value < 1_000_000_000)
            {
                BigInteger divided = value / 1_000;
                return ToString(divided) + "M";
            }
            else if (value < 1_000_000_000_000)
            {
                BigInteger divided = value / 1_000_000;
                return ToString(divided) + "B";
            }
            else
            {
                BigInteger divided = value / 1_000_000_000;
                return ToString(divided) + "T";
            }
        }

        private static string ToString(double value) {
            decimal d = (decimal)value;
            d = Math.Truncate(d * 1000m) / 1000m;
            string result = $"{d:0}";
            if (result.Length > 3)
            {
                result = result.Insert(result.Length - 3, ",");
            }
            return result;
        }

        private static string ToString(BigInteger value) {
            var str = value.ToString();
            if (str.Length > 3)
            {
                return str.Insert(str.Length - 3, ",");
            }
            return str;
        }
    }
}