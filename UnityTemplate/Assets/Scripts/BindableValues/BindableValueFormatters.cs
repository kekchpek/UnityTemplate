using System;
using System.Collections.Generic;
using BigInteger = System.Numerics.BigInteger;
using kekchpek.MVVM.Models.GameResources.Static;

namespace kekchpek.BindableValues
{
    internal static class BindableValueFormatters
    {
        private static readonly Dictionary<Type, Delegate> DefaultFormatters = new()
        {
            { typeof(int), new Func<int, string>(static value => ResourceFormatting.FormatNumber((double)value)) },
            { typeof(long), new Func<long, string>(static value => ResourceFormatting.FormatNumber((double)value)) },
            { typeof(float), new Func<float, string>(ResourceFormatting.FormatNumber) },
            { typeof(double), new Func<double, string>(ResourceFormatting.FormatNumber) },
            { typeof(BigInteger), new Func<BigInteger, string>(ResourceFormatting.FormatNumber) },
            { typeof(bool), new Func<bool, string>(static value => value.ToString()) },
        };

        public static Func<T, string> GetOrCreate<T>(Dictionary<Type, object> formatterCache)
        {
            if (formatterCache.TryGetValue(typeof(T), out var cachedFormatter))
            {
                return (Func<T, string>)cachedFormatter;
            }

            if (DefaultFormatters.TryGetValue(typeof(T), out var defaultFormatter))
            {
                formatterCache[typeof(T)] = defaultFormatter;
                return (Func<T, string>)defaultFormatter;
            }

            Func<T, string> formatter = static value => value?.ToString() ?? string.Empty;
            formatterCache[typeof(T)] = formatter;
            return formatter;
        }
    }
}
