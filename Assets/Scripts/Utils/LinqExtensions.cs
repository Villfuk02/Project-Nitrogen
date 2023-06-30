
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class LinqExtensions
    {
        public static T? ArgMin<T, TValue>(this IEnumerable<T> list, Func<T, TValue> mapping) where TValue : IComparable<TValue>
        {
            var enumerator = list.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Provided enumerable is empty.");

            T? result = enumerator.Current;
            TValue value = mapping(result);
            while (enumerator.MoveNext())
            {
                TValue v = mapping(enumerator.Current);
                if (v.CompareTo(value) < 0)
                {
                    value = v;
                    result = enumerator.Current;
                }
            }
            return result;
        }
        public static T? ArgMax<T, TValue>(this IEnumerable<T> list, Func<T, TValue> mapping) where TValue : IComparable<TValue>
        {
            var enumerator = list.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Provided enumerable is empty.");

            T? result = enumerator.Current;
            TValue value = mapping(result);
            while (enumerator.MoveNext())
            {
                TValue v = mapping(enumerator.Current);
                if (v.CompareTo(value) > 0)
                {
                    value = v;
                    result = enumerator.Current;
                }
            }
            return result;
        }
        public static IEnumerable<T>? EmptyToNull<T>(this IEnumerable<T> list)
        {
            if (list.Any())
                return list;
            else
                return null;
        }
    }
}
