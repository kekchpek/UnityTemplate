using System;
using System.Collections.Generic;

namespace kekchpek.Auxiliary.Collections.Extensions
{
    public static class SpanExtensions
    {
        public static bool Contains<T>(in this ReadOnlySpan<T> span, T item, IEqualityComparer<T> comparer = null)
        {
            foreach (var spanItem in span)
            {
                if ((comparer ?? EqualityComparer<T>.Default).Equals(spanItem, item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}